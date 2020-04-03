using System.Threading;
using System.Threading.Tasks;

namespace Terradue.WebService.Ogc.Core
{
	public interface IJob<T>
	{
		T Run();
	}
}