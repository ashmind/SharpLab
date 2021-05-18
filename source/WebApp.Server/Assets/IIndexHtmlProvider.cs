using System.Threading.Tasks;

namespace SharpLab.WebApp.Server.Assets {
    public interface IIndexHtmlProvider {
        Task<string> GetIndexHtmlContentAsync();
    }
}