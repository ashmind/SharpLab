namespace Fragile {
    public interface IProcessRunner {
        void InitialSetup();
        IProcessContainer StartProcess();
    }
}