using System.Threading.Tasks;

namespace AvaTagger.Models.Messages;

public record SaveFilePickerMessage(string Title, string DefaultExtension, string SuggestedFileName, TaskCompletionSource<string?> Completion);
