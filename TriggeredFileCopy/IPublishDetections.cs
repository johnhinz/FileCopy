using System.Threading;
using System.Threading.Tasks;

namespace TriggeredFileCopy
{
    public interface IPublishDetections<TResult>
    {
        public Task<TResult> PublishAsync<TPredictionType>
            (TPredictionType message, string source, CancellationToken token);
    }
}
