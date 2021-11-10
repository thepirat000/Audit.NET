using System;
using Audit.Core;

namespace Audit.Redis.Configuration
{
    internal class RedisSortedSetConfigurator : IRedisSortedSetConfigurator
    {
        internal Func<AuditEvent, string> _keyBuilder;
        internal TimeSpan? _timeToLive;
        internal Func<AuditEvent, double> _scoreBuilder;

        internal Func<AuditEvent, double> _maxScoreBuilder;
        internal bool _maxScoreExclusive;
        internal Func<AuditEvent, double> _minScoreBuilder;
        internal bool _minScoreExclusive;

        internal Func<AuditEvent, long> _maxRankBuilder;

        public IRedisSortedSetConfigurator Key(Func<AuditEvent, string> keyBuilder)
        {
            _keyBuilder = keyBuilder;
            return this;
        }

        public IRedisSortedSetConfigurator Key(string key)
        {
            _keyBuilder = ev => key;
            return this;
        }

        public IRedisSortedSetConfigurator TimeToLive(TimeSpan? timeToLive)
        {
            _timeToLive = timeToLive;
            return this;
        }

        public IRedisSortedSetConfigurator Score(Func<AuditEvent, double> scoreBuilder)
        {
            _scoreBuilder = scoreBuilder;
            return this;
        }

        public IRedisSortedSetConfigurator MaxScore(Func<AuditEvent, double> maxScoreBuilder, bool exclusive = false)
        {
            _maxScoreBuilder = maxScoreBuilder;
            _maxScoreExclusive = exclusive;
            return this;
        }

        public IRedisSortedSetConfigurator MaxScore(double maxScore, bool exclusive = false)
        {
            _maxScoreBuilder = ev => maxScore;
            _maxScoreExclusive = exclusive;
            return this;
        }

        public IRedisSortedSetConfigurator MinScore(Func<AuditEvent, double> minScoreBuilder, bool exclusive = false)
        {
            _minScoreBuilder = minScoreBuilder;
            _minScoreExclusive = exclusive;
            return this;
        }

        public IRedisSortedSetConfigurator MinScore(double minScore, bool exclusive = false)
        {
            _minScoreBuilder = ev => minScore;
            _minScoreExclusive = exclusive;
            return this;
        }

        public IRedisSortedSetConfigurator MaxRank(Func<AuditEvent, long> maxRankBuilder)
        {
            _maxRankBuilder = maxRankBuilder;
            return this;
        }

        public IRedisSortedSetConfigurator MaxRank(long maxRank)
        {
            _maxRankBuilder = ev => maxRank;
            return this;
        }
    }
}