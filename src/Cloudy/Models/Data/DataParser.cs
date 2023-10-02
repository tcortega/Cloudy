namespace Cloudy.Models.Data;

public delegate IDataParseResult<TInput> DataParser<out TInput>(string data) where TInput : ICredential;