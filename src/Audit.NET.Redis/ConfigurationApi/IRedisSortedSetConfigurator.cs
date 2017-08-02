using System;
using Audit.Core;

namespace Audit.Redis.Configuration
{
    /// <summary>
    /// Provides a configuration for the Redis SortedSet handler
    /// </summary>
    public interface IRedisSortedSetConfigurator
    {
        /// <summary>
        /// Specifies a function that returns the Redis Key to use.
        /// </summary>
        /// <param name="keyBuilder">A function that returns the Redis Key</param>
        IRedisSortedSetConfigurator Key(Func<AuditEvent, string> keyBuilder);
        /// <summary>
        /// Specifies the Redis Key to use.
        /// </summary>
        /// <param name="key">The Redis Key</param>
        IRedisSortedSetConfigurator Key(string key);
        /// <summary>
        /// Specifies the Time To Live for the entire Redis Key. Default is no TTL.
        /// </summary>
        /// <param name="timeToLive">The Time to Live</param>
        IRedisSortedSetConfigurator TimeToLive(TimeSpan? timeToLive);
        /// <summary>
        /// Specifies a function that returns the score for the sorted set member.
        /// </summary>
        /// <param name="scoreBuilder">A function that returns the score for the sorted set member</param>
        IRedisSortedSetConfigurator Score(Func<AuditEvent, double> scoreBuilder);
        /// <summary>
        /// Specifies a function that returns the maximum score allowed for the sorted set.
        /// Audits with score greater than the maximum will be deleted. This deletion takes place when a new event is stored.
        /// </summary>
        /// <param name="maxScoreBuilder">A function that returns the maximum score allowed</param>
        /// <param name="exclusive">Indicates if the maximum is an Exclusive range. Default is Inclusive.</param>
        IRedisSortedSetConfigurator MaxScore(Func<AuditEvent, double> maxScoreBuilder, bool exclusive = false);
        /// <summary>
        /// Specifies a Maximum Score allowed for the sorted set.
        /// Audits with score greater than the maximum will be deleted. This deletion takes place when a new event is stored.
        /// </summary>
        /// <param name="maxScore">The maximum score allowed</param>
        /// <param name="exclusive">Indicates if the maximum is an Exclusive range. Default is Inclusive.</param>
        IRedisSortedSetConfigurator MaxScore(double maxScore, bool exclusive = false);
        /// <summary>
        /// Specifies a function that returns the minimum score allowed for the sorted set.
        /// Audits with score less than the minimum will be deleted. This deletion takes place when a new event is stored.
        /// </summary>
        /// <param name="minScoreBuilder">A function that returns the minimum score allowed</param>
        /// <param name="exclusive">Indicates if the minimum is an Exclusive range. Default is Inclusive.</param>
        IRedisSortedSetConfigurator MinScore(Func<AuditEvent, double> minScoreBuilder, bool exclusive = false);
        /// <summary>
        /// Specifies the minimum score allowed for the sorted set.
        /// Audits with score less than the minimum will be deleted. This deletion takes place when a new event is stored.
        /// </summary>
        /// <param name="minScore">The minimum score allowed</param>
        /// <param name="exclusive">Indicates if the minimum is an Exclusive range. Default is Inclusive.</param>
        IRedisSortedSetConfigurator MinScore(double minScore, bool exclusive = false);
        /// <summary>
        /// Specifies a function that returns the maximum rank allowed for the sorted set. Use this to have a capped collection, to limit the members stored.
        /// If M is greater than zero: Maintain only the top M scored elements.
        /// If M is less than zero: Maintain only the bottom -M scored elements.
        /// </summary>
        /// <param name="maxRankBuilder">A function that returns the maximum rank allowed. Greater than zero to maintain the top M scored elements. Less than zero to maintain the bottom -M scored elements.</param>
        IRedisSortedSetConfigurator MaxRank(Func<AuditEvent, long> maxRankBuilder);
        /// <summary>
        /// Specifies the maximum rank allowed for the sorted set. Use this to have a capped collection, to limit the members stored.
        /// If M is greater than zero: Maintain only the top M scored elements.
        /// If M is less than zero: Maintain only the bottom -M scored elements.
        /// </summary>
        /// <param name="maxRank">The maximum rank allowed. Greater than zero to maintain the top M scored elements. Less than zero to maintain the bottom -M scored elements.</param>
        IRedisSortedSetConfigurator MaxRank(long maxRank);
    }
}