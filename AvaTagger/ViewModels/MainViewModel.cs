using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AvaTagger.Models;
using AvaTagger.Models.Messages;
using AvaTagger.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ObservableCollections;

namespace AvaTagger.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial bool Loading { get; set; }

    private readonly TaggerService _taggerService;
    private readonly IMessenger _messenger;
    private readonly ObservableList<TagsResultBox> _images = new();
    private readonly ObservableDictionary<string, ObservableCollection<TagsResultBox>> _groups = new();

    private readonly SemaphoreSlim _processLock = new(1);

    public ICollection<TagsResultBox> Images { get; }
    public ICollection<KeyValuePair<string, ObservableCollection<TagsResultBox>>> Groups { get; }

    public MainViewModel(TaggerService taggerService, IMessenger messenger)
    {
        _taggerService = taggerService;
        _messenger = messenger;
        Images = _images.ToNotifyCollectionChanged();
        Groups = _groups.ToNotifyCollectionChanged();
    }

    [RelayCommand]
    public void Test()
    {
        _taggerService.Initialize();
    }

    [RelayCommand]
    public async Task Initialize()
    {
        Loading = true;
        await Task.Run(() =>
        {
            _taggerService.Initialize();
        });
        Loading = false;
    }

    public async Task LoadImages(IEnumerable<string> imageFiles)
    {
        foreach (var item in imageFiles)
        {
            _images.Add(new TagsResultBox() { FilePath = item });
        }
        await _processLock.WaitAsync();
        try
        {
            var images = _images.ToArray();
            foreach (var item in images.Where(v => v.TagsResult == null && !v.IsError && !v.IsProgress))
            {
                item.IsProgress = true;
                TagsResult? result = null;
                try
                {
                    result = await Task.Run(() => _taggerService.Process(item.FilePath));
                    item.TagsResult = result;
                    item.IsProgress = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    item.IsError = true;
                    continue;
                }
                finally
                {
                    AddToGroup(item);
                }

                void AddToGroup(TagsResultBox tagsResultBox)
                {
                    var mainTag = tagsResultBox.TagsResult?.CharacterTags.FirstOrDefault().Name ?? string.Empty;
                    if (_groups.TryGetValue(mainTag, out var group))
                    {
                        group.Add(item);
                    }
                    else
                    {
                        _groups.Add(mainTag, new ObservableCollection<TagsResultBox>([tagsResultBox]));
                    }
                }
            }
        }
        finally
        {
            _processLock.Release();
        }
    }

    [RelayCommand]
    public async Task ExportImagesCopy()
    {
        var dir = await _messenger.Send<OpenFolderPickerMessage>(new("选择保存的目录", new())).Completion.Task;
        if (dir != null)
        {
            Directory.CreateDirectory(dir);
            foreach (var group in Groups)
            {
                var tagsDir = Path.Combine(dir, group.Key);
                Directory.CreateDirectory(tagsDir);
                foreach (var image in group.Value)
                {
                    File.Copy(image.FilePath, Path.Combine(tagsDir, Path.GetFileName(image.FilePath)));
                }
            }
        }
    }

    [RelayCommand]
    public async Task ExportImagesMove()
    {
        var dir = await _messenger.Send<OpenFolderPickerMessage>(new("选择移动的目录", new())).Completion.Task;
        if (dir != null)
        {
            Directory.CreateDirectory(dir);
            foreach (var group in Groups)
            {
                var tagsDir = Path.Combine(dir, group.Key);
                Directory.CreateDirectory(tagsDir);
                foreach (var image in group.Value)
                {
                    var target = Path.Combine(tagsDir, Path.GetFileName(image.FilePath));
                    File.Move(image.FilePath, target);
                    image.FilePath = target;
                }
            }
        }
    }


    [RelayCommand]
    public async Task ExportJson()
    {
        var filePath = await _messenger.Send<SaveFilePickerMessage>(new("保存到", "json", "tags.json", new())).Completion.Task;
        if (filePath != null)
        {
            var json = new JsonObject();
            foreach (var item in Images)
            {
                json.Add(item.FilePath, new JsonObject()
                {
                });
            }
            File.WriteAllText(filePath, json.ToJsonString(new System.Text.Json.JsonSerializerOptions()
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            }));
        }
    }
}
