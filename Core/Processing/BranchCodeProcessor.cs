using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;

namespace TryRoslyn.Core.Processing {
    public class BranchCodeProcessor : ICodeProcessor, IDisposable {
        private readonly string _branchName;
        private readonly IBranchProvider _branchProvider;

        private readonly Lazy<AppDomain> _branchAppDomain;
        private readonly Lazy<ICodeProcessor> _remoteProcessor;

        public BranchCodeProcessor(string branchName, IBranchProvider branchProvider) {
            _branchAppDomain = new Lazy<AppDomain>(CreateAppDomain);
            _remoteProcessor = new Lazy<ICodeProcessor>(CreateRemoteProcessor);
            _branchName = branchName;
            _branchProvider = branchProvider;
        }

        public ProcessingResult Process(string code) {
            return _remoteProcessor.Value.Process(code);
        }

        private AppDomain CreateAppDomain() {
            var coreAssemblyLocation = Uri.UnescapeDataString(new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath);

            var tempDirectory = new DirectoryInfo(Path.Combine(
                Path.GetDirectoryName(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile),
                @"App_Data\AppDomains", // ugly hack, just to save time
                _branchName
            ));
            if (tempDirectory.Exists)
                tempDirectory.Delete(true);

            tempDirectory.Create();

            var branchDirectory = _branchProvider.GetDirectory(_branchName);
            var originalDirectoryPath = Path.GetDirectoryName(coreAssemblyLocation);

            CopyFiles(branchDirectory.FullName, tempDirectory.FullName);
            CopyAndPrepareCoreAssembly(coreAssemblyLocation, tempDirectory);
            CopyFiles(originalDirectoryPath, tempDirectory.FullName);

            var domain = AppDomain.CreateDomain("Branch:" + _branchName, null, new AppDomainSetup {
                ApplicationBase = tempDirectory.FullName,
                ShadowCopyFiles = "true"
            });
            
            return domain;
        }

        private static void CopyFiles(string sourceDirectoryPath, string targetDirectoryPath) {
            foreach (var filePath in Directory.EnumerateFiles(sourceDirectoryPath)) {
                var targetPath = Path.Combine(targetDirectoryPath, Path.GetFileName(filePath));
                if (File.Exists(targetPath))
                    continue;

                File.Copy(filePath, targetPath);
            }
        }

        private static void CopyAndPrepareCoreAssembly(string originalLocation, DirectoryInfo tempDirectory) {
            var newLocation = Path.Combine(tempDirectory.FullName, Path.GetFileName(originalLocation));
            File.Copy(originalLocation, newLocation);

            var newAssembly = AssemblyDefinition.ReadAssembly(newLocation);
            foreach (var reference in newAssembly.MainModule.AssemblyReferences) {
                var fileInTemp = GetAssemblyFile(tempDirectory.FullName, reference);
                if (!fileInTemp.Exists)
                    continue;

                var assemblyName = AssemblyName.GetAssemblyName(fileInTemp.FullName);
                if (assemblyName.GetPublicKey() != null)
                    continue;

                reference.PublicKey = null;
                reference.PublicKeyToken = null;
                reference.HasPublicKey = false;
            }
            newAssembly.Write(newLocation);
        }

        private static FileInfo GetAssemblyFile(string directoryPath, AssemblyNameReference name) {
            // this is naive, but I do not think there is a reason to overcomplicae it
            return new FileInfo(Path.Combine(directoryPath, name.Name + ".dll"));
        }
        
        private ICodeProcessor CreateRemoteProcessor() {
            return (ICodeProcessor)_branchAppDomain.Value.CreateInstanceAndUnwrap(
                GetType().Assembly.FullName,
                typeof(CodeProcessorProxy).FullName
            );
        }

        public void Dispose() {
            if (!_branchAppDomain.IsValueCreated)
                return;

            AppDomain.Unload(_branchAppDomain.Value);
        }
    }
}
