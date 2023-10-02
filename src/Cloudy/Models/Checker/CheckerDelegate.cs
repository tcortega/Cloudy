using Cloudy.Http;
using Cloudy.Models.Data;

namespace Cloudy.Models.Checker;

public delegate Task<CheckResult> CheckerDelegate<TInput>(BotData<TInput> botData, CloudyHttpClient client) where TInput : ICredential;