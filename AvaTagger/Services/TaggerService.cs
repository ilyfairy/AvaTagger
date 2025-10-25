using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Advanced;
using CsvHelper;
using System.Text.Json;
using System.IO;
using System.Text.Json.Serialization;

namespace AvaTagger.Services;


public class TaggerService : IDisposable
{
    private CaformerDbv4Inference _caformerDbv4Inference = new();

    public void Initialize()
    {
        _caformerDbv4Inference.Initialize();
    }

    public TagsResult Process(string imageFilePath)
    {
        using FileStream fs = new(imageFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return _caformerDbv4Inference.Inference(fs);
    }

    public TagsResult Process(Stream imageStream)
    {
        return _caformerDbv4Inference.Inference(imageStream);
    }

    public void Dispose()
    {
        _caformerDbv4Inference.Dispose();
    }
}


public class TimmConfig
{
    [JsonPropertyName("tags")]
    public string[] Tags { get; set; } = [];
}

public class TagRecord
{
    [CsvHelper.Configuration.Attributes.Name("name")]
    public string Name { get; set; } = string.Empty;

    [CsvHelper.Configuration.Attributes.Name("category")]
    public int Category { get; set; }

    [CsvHelper.Configuration.Attributes.Name("best_threshold")]
    public float BestThreshold { get; set; }
}

public readonly record struct PredictedTag(string Name, float Probability);
public record TagsResult(IReadOnlyList<PredictedTag> GeneralTags, IReadOnlyList<PredictedTag> CharacterTags, IReadOnlyList<PredictedTag> RatingTags);

/// <summary>
/// https://huggingface.co/animetimm/caformer_b36.dbv4-full
/// </summary>
public class CaformerDbv4Inference : IDisposable
{
    private static readonly float[] Mean = [0.485f, 0.456f, 0.406f];
    private static readonly float[] Std = [0.229f, 0.224f, 0.225f];
    private const int PadSize = 512;
    private const int FinalSize = 384;

    private Dictionary<string, TagRecord> _tagsMetadata = null!;
    private TimmConfig _timmConfig = null!;
    private InferenceSession _session = null!;

    private bool _initialized = false;

    public DenseTensor<float> Preprocess(Stream imageStream)
    {
        using var image = Image.Load<Rgba32>(imageStream);

        int targetSize = Math.Max(Math.Max(image.Width, image.Height), PadSize);
        using var padded = new Image<Rgba32>(targetSize, targetSize, new Rgba32(255, 255, 255, 255));

        int offsetX = (targetSize - image.Width) / 2;
        int offsetY = (targetSize - image.Height) / 2;

        padded.Mutate(x => x.DrawImage(image, new Point(offsetX, offsetY), 1.0f));
        padded.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(FinalSize, FinalSize),
            Mode = ResizeMode.Stretch,
            Sampler = KnownResamplers.Bicubic
        }));

        Rectangle cropRect = new(
            (padded.Width - FinalSize) / 2, (padded.Height - FinalSize) / 2,
            FinalSize, FinalSize
        );
        padded.Mutate(x => x.Crop(cropRect));

        var tensor = new DenseTensor<float>([1, 3, FinalSize, FinalSize]); // BCHW
        for (int y = 0; y < FinalSize; y++)
        {
            Span<Rgba32> rowSpan = padded.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < FinalSize; x++)
            {
                var px = rowSpan[x];
                float r = px.R / 255f;
                float g = px.G / 255f;
                float b = px.B / 255f;
                tensor[0, 0, y, x] = (r - Mean[0]) / Std[0];
                tensor[0, 1, y, x] = (g - Mean[1]) / Std[1];
                tensor[0, 2, y, x] = (b - Mean[2]) / Std[2];
            }
        }
        return tensor;
    }


    public void Initialize()
    {
        if (_initialized)
            return;

        string tagsCsv = "selected_tags.csv";
        _tagsMetadata = LoadTagMeta(tagsCsv).ToDictionary(x => x.Name);

        string configPath = "config.json";
        using var configFileStream = new FileStream(configPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        _timmConfig = JsonSerializer.Deserialize<TimmConfig>(configFileStream)!;

        //var sessionOptions = new SessionOptions();
        //sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        //sessionOptions.AppendExecutionProvider_DML(0);
        string modelPath = "model.onnx";
        _session = new InferenceSession(modelPath);

        _initialized = true;
    }

    public List<TagRecord> LoadTagMeta(string csvPath)
    {
        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        return [.. csv.GetRecords<TagRecord>()];
    }

    public TagsResult Inference(Stream imageStream)
    {
        var inputTensor = Preprocess(imageStream);
        var outputMap = _timmConfig.Tags;
        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input", inputTensor) };
        using var results = _session.Run(inputs);
        float[] probs = results.First(x => x.Name == "prediction").AsEnumerable<float>().ToArray();
        var probs_with_label = probs.Zip(outputMap).OrderByDescending(x => x.First).ToList();
        return new TagsResult(
            probs_with_label.Select(v => new PredictedTag(v.Second, v.First)).Where(x => _tagsMetadata[x.Name].Category == 0 && x.Probability > _tagsMetadata[x.Name].BestThreshold).Take(50).ToList(),
            probs_with_label.Select(v => new PredictedTag(v.Second, v.First)).Where(x => _tagsMetadata[x.Name].Category == 4 && x.Probability > _tagsMetadata[x.Name].BestThreshold).Take(30).ToList(),
            probs_with_label.Select(v => new PredictedTag(v.Second, v.First)).Where(x => _tagsMetadata[x.Name].Category == 9).ToList()
        );
    }

    public void Dispose()
    {
        _session.Dispose();
    }
}
