using System.DirectoryServices.Protocols;
using AD.Protocols.ADObjects.Objects;
using SlugEnt.FluentResults;

namespace AD.Protocols.ADObjects.Fields;


/// <summary>
/// Used to store and manipulate an Active Directory attribute value that is multi-valued and contains Distinguished Names (DNs).
/// This class allows for the addition and removal of DNs from the attribute, as well as tracking the current values of the attribute.
/// </summary>
public class MultiValuedDNAttribute
{
    internal HashSet<string> Additions { get; set; } = new HashSet<string>();
    internal HashSet<string> Removals { get; set; } = new HashSet<string>();    
    internal HashSet<string> CurrentValues { get; set; } = new HashSet<string>();

    /// <summary>
    /// If true. the attribute will allow users to be added.
    /// </summary>
    public bool AllowUsers { get; set; } = true;

    /// <summary>
    /// If true, the attribute will allow groups to be added.
    /// </summary>
    public bool AllowGroups { get; set; } = true;
    
    
    /// <summary>
    /// Initializes the attribute with existing values from Active Directory.
    /// </summary>
    /// <param name="initialValues">A collection of existing DNs for this attribute.</param>
    public void Initialize(IEnumerable<string> initialValues) { CurrentValues = new HashSet<string>(initialValues); }


    /// <summary>
    /// Adds a Distinguished Name (DN) to the attribute.
    /// This addition will be staged for saving to Active Directory.
    /// </summary>
    /// <param name="dn">The DN to add.</param>
    /// <returns>True if the DN was successfully added or already exists, false otherwise.</returns>
    public bool AddMember(string dn)
    {
        if (string.IsNullOrWhiteSpace(dn))
            return false;

        // If the item is already in CurrentValues, it means it's already in AD and we don't need to add it.
        if (CurrentValues.Contains(dn))
            return true;

        // If the item is marked for removal, we need to unmark it and add it.
        if (Removals.Contains(dn))
        {
            Removals.Remove(dn);

            // We don't add to Additions because it will be added back implicitly by not being removed.
            return true;
        }

        // If it's not in current values and not marked for removal, add it to the additions list.
        return Additions.Add(dn);
    }


    /// <summary>
    /// Adds an ADpUser object to the attribute IF AllowUsers is set to true.  If AllowUsers is false, an exception is thrown.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public bool AddMember(ADpUser user)
    {
        if (AllowUsers)
            return AddMember(user.DistinguishedName);
        
        throw new ArgumentException("Adding users is not allowed for this attribute.");
    }


    /// <summary>
    /// Adds an ADpGroup object to the attribute IF AllowGroups is set to true.  If AllowGroups is false, an exception is thrown.
    /// </summary>
    /// <param name="group"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public bool AddMember(ADpGroup group)
    {
        if (AllowGroups)
            return AddMember(group.DistinguishedName);
        
        throw new ArgumentException("Adding groups is not allowed for this attribute.");
    }
    

    /// <summary>
    /// Removes a Distinguished Name (DN) from the attribute.
    /// This removal will be staged for saving to Active Directory.
    /// </summary>
    /// <param name="dn">The DN to remove.</param>
    /// <param name="forceRemove">If true, forces the removal even if the DN is not present in the currentValues (ie, you did not download the current values from AD - but want it removed.).</param>
    /// <returns>True if the DN was successfully removed or was not present, false otherwise.</returns>
    public bool RemoveMember(string dn, bool forceRemove)
    {
        if (string.IsNullOrWhiteSpace(dn))
            return false;

        // If the item is in Additions, it means it was added locally but not yet committed to AD.
        // In this case, we can simply remove it from Additions.
        if (Additions.Contains(dn))
        {
            Additions.Remove(dn);
            return true;
        }

        // If the item is in CurrentValues, it means it's currently in AD.
        // Mark it for removal.
        if (CurrentValues.Contains(dn) || forceRemove)
        {
            Removals.Add(dn);
            return true;
        }

        // If the item is neither in Additions nor CurrentValues, it means it's not present and
        // doesn't need to be removed from AD.
        return false;
    }


    /// <summary>
    /// Removes a user from this attribute, IF AllowUsers is set to true.  If AllowUsers is false, an exception is thrown.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="forceRemove"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public bool RemoveMember (ADpUser user, bool forceRemove)
    {
        if (AllowUsers)
            return RemoveMember(user.DistinguishedName, forceRemove);

        throw new ArgumentException("Removing users is not allowed for this attribute.");
    }


    /// <summary>
    /// Removes a group from this attribute, IF AllowGroups is set to true.  If AllowGroups is false, an exception is thrown.
    /// </summary>
    /// <param name="group"></param>
    /// <param name="forceRemove"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public bool RemoveMember(ADpGroup group,
                             bool forceRemove)
    {
        if (AllowGroups)
            return RemoveMember(group.DistinguishedName, forceRemove);

        throw new ArgumentException("Removing groups is not allowed for this attribute.");
    }
    
    
    /// <summary>
    /// Gets a collection of all current values for the attribute, including staged additions and removals.
    /// </summary>
    public IEnumerable<string> GetCurrentAndStagedValues()
    {
        var effectiveValues = new HashSet<string>(CurrentValues);

        foreach (var added in Additions)
        {
            effectiveValues.Add(added);
        }

        foreach (var removed in Removals)
        {
            effectiveValues.Remove(removed);
        }

        return effectiveValues;
    }


    /// <summary>
    /// Clears all staged additions and removals, reverting the attribute to its last saved state.
    /// </summary>
    public void DiscardChanges()
    {
        Additions.Clear();
        Removals.Clear();
    }
    
    
    /// <summary>
    /// Name of the Attribute in Active Directory that this object is managing.
    /// </summary>
    public string AttributeName { get; protected set; }

    /// <summary>
    /// Maximum number of values to retrieve per Active Directory query.  Active Directory has a limit of 1500 values per query,
    /// so this value should be set to 1500 or less.  The default is 500.
    /// </summary>
    public int MaxValuesToRetrievePerADQuery { get; set; } = 500;

    /// <summary>
    /// Constructs a new MultiValuedDNAttribute object with the specified attribute name.
    /// </summary>
    /// <param name="attributeName">The name of the Active Directory attribute.</param>
    /// <param name="allowUsers">Indicates whether users are allowed for this attribute.</param>
    /// <param name="allowGroups">Indicates whether groups are allowed for this attribute.</param>
    public MultiValuedDNAttribute(string attributeName, bool allowUsers, bool allowGroups) 
    { 
        AttributeName = attributeName; 
        AllowUsers = allowUsers;
        AllowGroups = allowGroups;
    }


    /// <summary>
    /// Saves the requested changes (additions and removals) to the Active Directory attribute using the provided LDAP connection.
    /// </summary>
    /// <param name="ldapConnection">The LDAP connection to use for the modification request.</param>
    /// <param name="parentDistinguishedName">The distinguished name (DN) of the parent object in Active Directory.</param>
    /// <returns>A Result indicating success or failure of the operation.</returns>
    internal virtual Result SaveChangesToActiveDirectory(LdapConnection ldapConnection, string parentDistinguishedName)
    {
        // If the object has members that were added or removed, process them here.
        if (Additions.Count > 0 || Removals.Count > 0)
        {
            try
            {
                var request = new ModifyRequest(parentDistinguishedName);

                // Process Adds
                if (Additions.Count > 0)
                {
                    DirectoryAttributeModification memberModification = new DirectoryAttributeModification
                    {
                        Name = AttributeName,
                        Operation = DirectoryAttributeOperation.Add
                    };

                    foreach (string member in Additions)
                    {
                        memberModification.Add(member);
                    }

                    request.Modifications.Add(memberModification);
                }

                if (Removals.Count > 0)
                {
                    DirectoryAttributeModification memberModification = new DirectoryAttributeModification
                    {
                        Name = AttributeName,
                        Operation = DirectoryAttributeOperation.Delete
                    };

                    foreach (string member in Removals)
                    {
                        memberModification.Add(member);
                    }
                    request.Modifications.Add(memberModification);
                }


                ModifyResponse response = (ModifyResponse)ldapConnection.SendRequest(request);
                if (response.ResultCode == ResultCode.Success)
                {
                    foreach (string member in Additions)
                    {
                        CurrentValues.Add(member);
                    }
                    foreach (string member in Removals)
                    {
                        CurrentValues.Remove(member);
                    }
                    Removals.Clear();
                    Additions.Clear();
                    return Result.Ok();
                }

                // Failure.  
                return Result.Fail(response.ErrorMessage);
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }
        return Result.Ok();
    }


    /// <summary>
    /// Provides a method to retieve all members of an Active Directory Attribute that may contain more than the maximum 1500 AD members that can be returned in a single query.
    /// This method will retrieve all members of the attribute and return them in the passed in HashSet.  Note any members in the current Hashset are cleared.
    /// </summary>
    /// <param name="parentDistinguishedName">The Full Distinguished Name of the object to retrieve the attribute from.</param>
    /// <param name="ldapConnection">The LDAP connection to use for the query.</param>
    /// <param name="stepCount">The number of members to retrieve per request.</param>
    /// <returns></returns>
    internal Result GetMembersFromActiveDirectory(string parentDistinguishedName, LdapConnection ldapConnection)
    {
        int startRange = 0;
        bool hasMoreMembers = true;

        HashSet<string> retrievedMembers = new HashSet<string>();

        while (hasMoreMembers)
        {
            int endRange = startRange + MaxValuesToRetrievePerADQuery - 1;

            // Format the range attribute query (e.g., "member;range=0-1499")
            string memberAttributeWithRange = $"{AttributeName};range={startRange}-{endRange}";

            var request = new SearchRequest(
                                            parentDistinguishedName,
                                            "(objectClass=*)",
                                            SearchScope.Base, // Base scope targets only this specific group object
                                            new string[]
                                            {
                                                memberAttributeWithRange
                                            }
                                           );

            var response = (SearchResponse)ldapConnection.SendRequest(request);

            // If nothing found, exit the while loop.
            if (response.Entries.Count == 0)
                break;

            SearchResultEntry groupEntry = response.Entries[0];
            bool rangeFoundInThisLoop = false;


            foreach (string attrName in groupEntry.Attributes.AttributeNames)
            {
                // Active Directory will return either "member;range=X-Y" or "member;range=X-*"
                if (attrName.StartsWith($"{AttributeName};range=", StringComparison.OrdinalIgnoreCase))
                {
                    rangeFoundInThisLoop = true;
                    DirectoryAttribute attribute = groupEntry.Attributes[attrName];

                    // Extract Distinguished Names of the members
                    foreach (object val in attribute.GetValues(typeof(string)))
                    {
                        retrievedMembers.Add(val.ToString());
                    }

                    // If the attribute name ends with "-*", we have reached the final block
                    if (attrName.EndsWith("-*"))
                    {
                        hasMoreMembers = false;
                    }
                    else
                    {
                        startRange += MaxValuesToRetrievePerADQuery;
                    }

                    break;
                }
            }

            // Fallback: If the group has < MaxValuesToRetrievePerADQuery members
            if (!rangeFoundInThisLoop)
            {
                if (groupEntry.Attributes.Contains(AttributeName))
                {
                    DirectoryAttribute attribute = groupEntry.Attributes[AttributeName];
                    foreach (object val in attribute.GetValues(typeof(string)))
                    {
                        retrievedMembers.Add(val.ToString());
                    }
                }

                hasMoreMembers = false;
            }
        }

        CurrentValues = retrievedMembers;
        return Result.Ok();
    }


}

