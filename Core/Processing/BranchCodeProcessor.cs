using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AshMind.IO.Abstractions;
using JetBrains.Annotations;
using Mono.Cecil;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;

namespace TryRoslyn.Core.Processing {
    [ThreadSafe]
    public class BranchCodeProcessor : ICodeProcessor {
        private readonly string _branchName;
        private readonly IBranchProvider _branchProvider;
        // ReSharper disable once AgentHeisenbug.FieldOfNonThreadSafeTypeInThreadSafeType
        private readonly IFileSystem _fileSystem;

        // ReSharper disable once AgentHeisenbug.MutableFieldInThreadSafeType
        private bool _disposed;

        // ReSharper disable once AgentHeisenbug.FieldOfNonThreadSafeTypeInThreadSafeType
        private readonly Lazy<AppDomain> _branchAppDomain;
        private readonly Lazy<ICodeProcessor> _remoteProcessor;

        public BranchCodeProcessor(string branchName, IBranchProvider branchProvider, IFileSystem fileSystem) {
            _branchAppDomain = new Lazy<AppDomain>(CreateAppDomain);
            _remoteProcessor = new Lazy<ICodeProcessor>(CreateRemoteProcessor);
            _branchName = branchName;
            _branchProvider = branchProvider;
            _fileSystem = fileSystem;
        }

        public ProcessingResult Process(string code, ProcessingOptions options = null) {
            return _remoteProcessor.Value.Process(code, options);
        }

        private AppDomain CreateAppDomain() {
            var coreAssemblyFile = _fileSystem.GetFile(
                Uri.UnescapeDataString(new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath)
            );

            var tempDirectory = _fileSystem.GetDirectory(Path.Combine(
                Path.GetDirectoryName(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile),
                @"App_Data\AppDomains", // ugly hack, just to save time
                _branchName
            ));
            if (tempDirectory.Exists) {
                try {
                    tempDirectory.Delete(true);
                }
                catch (UnauthorizedAccessException) {
                    throw;
                }
            }

            tempDirectory.Create();

            var branchDirectory = _branchProvider.GetDirectory(_branchName);
            var originalDirectory = coreAssemblyFile.Directory;

            CopyFiles(branchDirectory, tempDirectory);
            CopyAndPrepareCoreAssembly(coreAssemblyFile, tempDirectory);
            CopyFiles(originalDirectory, tempDirectory);

            var domain = AppDomain.CreateDomain("Branch:" + _branchName, null, new AppDomainSetup {
                ApplicationBase = tempDirectory.FullName
            });
            
            return domain;
        }

        private static void CopyFiles(IDirectory sourceDirectory, IDirectory targetDirectory) {
            foreach (var sourceFile in sourceDirectory.EnumerateFiles()) {
                var targetFile = targetDirectory.GetFile(sourceFile.Name);
                if (targetFile.Exists)
                    continue;

                sourceFile.CopyTo(targetFile.FullName);
            }
        }

        private void CopyAndPrepareCoreAssembly(IFile originalAssemblyFile, IDirectory tempDirectory) {
            var newLocation = tempDirectory.GetFile(originalAssemblyFile.Name);
            originalAssemblyFile.CopyTo(newLocation.FullName);

            AssemblyDefinition newAssembly;
            using (var stream = newLocation.OpenRead()) {
                newAssembly = AssemblyDefinition.ReadAssembly(stream);
            }
            foreach (var reference in newAssembly.MainModule.AssemblyReferences) {
                var fileInTemp = GetAssemblyFile(tempDirectory, reference);
                if (!fileInTemp.Exists)
                    continue;

                var assemblyName = AssemblyName.GetAssemblyName(fileInTemp.FullName);
                if (assemblyName.GetPublicKey() != null)
                    continue;

                reference.PublicKey = null;
                reference.PublicKeyToken = null;
                reference.HasPublicKey = false;
            }
            using (var stream = newLocation.Open(FileMode.Create)) {
                newAssembly.Write(stream);
            }
        }

        [Pure]
        private IFile GetAssemblyFile(IDirectory directory, AssemblyNameReference name) {
            // this is naive, but I do not think there is a reason to overcomplicate it
            return directory.GetFile(name.Name + ".dll");
        }
        
        [Pure]
        private ICodeProcessor CreateRemoteProcessor() {
            return (ICodeProcessor)_branchAppDomain.Value.CreateInstanceAndUnwrap(
                GetType().Assembly.FullName,
                typeof(CodeProcessorProxy).FullName
            );
        }

        public void Dispose() {
            if (_disposed)
                return;

            _disposed = true;
            if (!_branchAppDomain.IsValueCreated)
                return;

            AppDomain.Unload(_branchAppDomain.Value);
        }
    }
}
