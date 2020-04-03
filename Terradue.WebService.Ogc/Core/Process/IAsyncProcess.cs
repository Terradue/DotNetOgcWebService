using System.Threading.Tasks;

namespace Terradue.WebService.Ogc.Core
{
    public interface IAsyncProcess : IProcess
    {

        string Description { get;  }

        Task Task { get; }


    }
}