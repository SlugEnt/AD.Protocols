
using SlugEnt.AD.Protocols;

namespace AD.Protocols.ADObjects;


/// <summary>
/// Manages the attributes the user want to retrieve from Active Directory when querying AD.
/// The class is optimized to only create the final list when the list is requested and only
/// do it once or anytime the list is changed.
/// </summary>
public class AttributeRetrieverMgr
{
    private List<string> _attributesToRetrieve = new List<string>();
    private string[] _attributesToReturn;

    
    /// <summary>
    /// Constructor
    /// </summary>
    public AttributeRetrieverMgr()
    {
        AddRequiredParameters();
    }
    
    
    /// <summary>
    /// Retrieves the list of attributes to retrieve as an array of strings.
    /// </summary>
    public string[] Attributes {
        get
        {
            if (!IsFinalized)
                Finalize();
            return _attributesToReturn;
        }}

    
    /// <summary>
    /// Used to ensure the Attributes to Return is synced to the Attributes To Retrieve.
    /// </summary>
    internal bool IsFinalized { get; private set; } = false;


    /// <summary>
    /// Returns the number of attributes currently in the list of attributes to retrieve.
    /// </summary>
    public int Count {get {return _attributesToRetrieve.Count; }}
    
    /// <summary>
    ///   Adds an attribute to the list of attributes to retrieve.
    /// If the attribute is already in the list, it will not be added again.
    /// </summary>
    /// <param name="attributeName"></param>
    public void AddAttribute(string attributeName)
    {
        if (!_attributesToRetrieve.Contains(attributeName))
        {
            _attributesToRetrieve.Add(attributeName);
            IsFinalized = false;
        }
    }


    /// <summary>
    /// Clears the list of attributes to retrieve.  Resets the finalized state to false so that the list will be rebuilt when requested.
    /// </summary>
    public void Clear()
    {
        _attributesToRetrieve.Clear();
        _attributesToReturn = Array.Empty<string>();
        
        // These must always be included in the list of attributes to retrieve.
        AddRequiredParameters();
        
        IsFinalized         = false;
    }


    /// <summary>
    /// Locks the 
    /// </summary>
    public void Finalize()
    {
        _attributesToReturn = _attributesToRetrieve.ToArray();
        IsFinalized = true;
    }


    /// <summary>
    /// Attributes that must be in every retrieval.
    /// </summary>
    internal void AddRequiredParameters()
    {
        // These are required attributes and must always be included.
        AddAttribute(ADpCommon.ATN_DISTINGUISHED_NAME);
        AddAttribute(ADpCommon.ATN_NAME);
    }
}

