namespace Fragile {
    public interface IProcessRunner {
        void InitialSetup();
        IProcessContainer StartProcess(bool DEBUG_suspended = false);
    }
}