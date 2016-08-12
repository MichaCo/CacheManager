using CacheManager.Core.Logging;

namespace CacheManager.Logging.SeriLog
{
    public class SeriLogFactory : ILoggerFactory
    {

        public ILogger CreateLogger(string categoryName)
        {
            return new SeriLogger(categoryName);
        }

        public ILogger CreateLogger<T>(T instance)
        {
            return new SeriLogger();
        }
    }
}
