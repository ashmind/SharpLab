using System;
using System.IO;
using System.Reflection;
using AppDomainToolkit;
using AshMind.Extensions;
using Microsoft.IO;
using Unbreakable;

namespace SharpLab.Server.Execution {
    public class Executor {
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;

        public Executor(RecyclableMemoryStreamManager memoryStreamManager) {
            _memoryStreamManager = memoryStreamManager;
        }

        public string Execute(Stream assemblyStream) {
            using (var guardedStream = _memoryStreamManager.GetStream()) {
                RuntimeGuardToken guardToken;
                using (assemblyStream) {
                    guardToken = AssemblyGuard.Rewrite(assemblyStream, guardedStream);
                }

                var currentSetup = AppDomain.CurrentDomain.SetupInformation;
                using (var context = AppDomainContext.Create(new AppDomainSetup {
                    ApplicationBase = currentSetup.ApplicationBase,
                    PrivateBinPath = currentSetup.PrivateBinPath
                })) {
                    context.LoadAssembly(LoadMethod.LoadFrom, Assembly.GetExecutingAssembly().GetAssemblyFile().FullName);
                    return RemoteFunc.Invoke(context.Domain, guardedStream, guardToken, Remote.Execute);
                }
            }
        }

        private static class Remote {
            public static string Execute(Stream assemblyStream, RuntimeGuardToken guardToken) {
                try {
                    var assembly = Assembly.Load(ReadAllBytes(assemblyStream));
                    var c = assembly.GetType("C");
                    var m = c.GetMethod("M");

                    using (guardToken.Scope()) {
                        return m.Invoke(Activator.CreateInstance(c), null)?.ToString();
                    }
                }
                catch (Exception ex) {
                    return ex.ToString();
                }
            }

            private static byte[] ReadAllBytes(Stream stream) {
                byte[] bytes;
                if (stream is MemoryStream memoryStream) {
                    bytes = memoryStream.GetBuffer();
                    if (bytes.Length != memoryStream.Length)
                        bytes = memoryStream.ToArray();
                    return bytes;
                }

                // we can't use ArrayPool here as this method is called in a temp AppDomain
                bytes = new byte[stream.Length];
                if (stream.Read(bytes, 0, (int)stream.Length) != bytes.Length)
                    throw new NotSupportedException();

                return bytes;
            }
        }
    }
}
