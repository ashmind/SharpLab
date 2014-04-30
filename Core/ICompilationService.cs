namespace TryRoslyn.Core {
    public interface ICompilationService {
        ProcessingResult Process(string code);
    }
}