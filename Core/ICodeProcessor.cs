namespace TryRoslyn.Core {
    public interface ICodeProcessor {
        ProcessingResult Process(string code);
    }
}