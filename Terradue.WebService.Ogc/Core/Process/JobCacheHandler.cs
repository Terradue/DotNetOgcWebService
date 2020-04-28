using System;
namespace Terradue.WebService.Ogc.Core {
    public abstract class JobCacheHandler {

        public JobCacheHandler() {
        }

        public virtual void Save(string uid, JobOrder order) {

        }

        public virtual void Load(string uid, JobOrder order) {

        }
    }
}
