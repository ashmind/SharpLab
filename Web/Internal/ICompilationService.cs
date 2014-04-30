namespace TryRoslyn.Web.Internal {
    public interface ICompilationService {
        ProcessingResult Process(string code);
    }
}