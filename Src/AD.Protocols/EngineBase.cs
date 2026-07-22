using Microsoft.Extensions.Logging;
using SlugEnt.AD.Protocols.DatetimeProvider;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("HRNextGen_Test")]

namespace SlugEnt.AD.Protocols;


/// <summary>
///     The base engine for all Entity Engines.  Basically just provides DateTimeOffset providers.
/// </summary>
public abstract class EngineBase
{
    /// <summary>
    ///     Constructor for Engine
    /// </summary>
    public EngineBase()    
    {
        // Provide for alternative DateTime implementations (mainly for unit testing)
        DateTimeOffsetProvider = new DateTimeOffsetProvider();
        DateTimeProvider = new DateTimeProvider();
    }
    

    /// <summary>
    /// Construcctor that accepts the ILogger
    /// </summary>
    /// <param name="logger"></param>
    public EngineBase(ILogger<EngineBase> logger) : this()
    {
        _logger = logger;
    }


    /// <summary>
    ///     Allows for the replacement of the DateTimeOffset Provider for unit testing purposes.
    /// </summary>
    public IDateTimeOffsetProvider DateTimeOffsetProvider { get; set; }


    /// <summary>
    /// Allows for the replacement of the DateTime Provider for unit testing purposes.
    /// </summary>
    public IDateTimeProvider DateTimeProvider { get; set; }


    /// <summary>
    /// The Logger for the engine
    /// </summary>
    protected ILogger<EngineBase> _logger { get; set; }

}