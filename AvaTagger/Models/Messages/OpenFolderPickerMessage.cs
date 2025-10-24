using System.Threading.Tasks;

namespace AvaTagger.Models.Messages;

public record OpenFolderPickerMessage(string Title, TaskCompletionSource<string?> Completion);
