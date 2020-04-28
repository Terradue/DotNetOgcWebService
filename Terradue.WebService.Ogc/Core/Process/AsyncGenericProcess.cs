using System;
using System.Threading.Tasks;

namespace Terradue.WebService.Ogc.Core
{
    public class AsyncGenericProcess : IAsyncProcess
    {
        readonly Task task;
        readonly string description;

        public AsyncGenericProcess(Task task, string description){
            this.description = description;
            this.task = task;
        }

        public string Description => description;

        public Task Task => task;

        public string Id {
            get {
                return Task.Id.ToString();
            }
        }

		public string Version
        {
			get;
        }
    }
}
