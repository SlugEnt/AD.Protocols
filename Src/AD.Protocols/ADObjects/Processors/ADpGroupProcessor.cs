using AD.Protocols.ADObjects.Objects;
using SlugEnt.AD.Protocols;
using SlugEnt.FluentResults;
using System.DirectoryServices.Protocols;

namespace AD.Protocols.ADObjects.Processors;

/// <summary>
/// Allows for the processing of Active Directory Group objects.  This includes creating, reading, updating, and deleting groups in Active Directory.
/// </summary>
public class ADpGroupProcessor : ADpGenericProcessor<ADpGroup>
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="ldapConnection"></param>
    /// <param name="addGroupDefaultAttributes">If true, the set of attributes which this library considers the default set of attributes to retrieve from AD for each group object
    /// are set now.  If you wish to completely customize this list, you can set this to false OR after processor creation, clear the list and set your own.</param>
    public ADpGroupProcessor(LdapConnection ldapConnection, bool addGroupDefaultAttributes = true) : base(ADpCommon.OBJ_CLASS_GROUP, "Group", ldapConnection) 
    { 
        if (addGroupDefaultAttributes)
            AttrRetrieval_Default();
    }



    protected override Result<ADpGroup> CreateObjectFromAttributes(SearchResultAttributeCollection attributes)
    {
        ADpGroup group = new(attributes);
        return Result.Ok(group);
    }


    /// <summary>
    /// Set Default Attributes to be retrieved if none are defined at time of retrieval from AD
    /// DisplayName, sAMAccountName, Description, CN and DistinguishedName
    /// </summary>
    internal override void AttrRetrieval_Default()
    {
        AttributeRetrieverMgr.AddAttribute("displayName");
        AttributeRetrieverMgr.AddAttribute("sAMAccountName");
        AttributeRetrieverMgr.AddAttribute("description");
        AttributeRetrieverMgr.AddAttribute("groupType");
        AttributeRetrieverMgr.AddAttribute("mail");
    }


    /// <summary>
    /// How many members are retrieved per request.  Active Directory has a limit of 1500 members per request, so this value should be set to 1500 or less.  The default is 1400.
    /// </summary>
    internal int MembersRetrievedPerRequest { get; set; } = 1400;


    /// <summary>
    /// Retrieves all members of the specified group from Active Directory.  This method handles the range retrieval mechanism used by Active Directory for groups with a large number of members (more than 1500).
    /// It will continue to retrieve members in batches until all members have been retrieved.
    /// </summary>
    /// <param name="group"></param>
    /// <returns></returns>
    public Result GetMembers(ADpGroup group)
    {
        int  step           = MembersRetrievedPerRequest;
        int  startRange     = 0;
        bool hasMoreMembers = true;

        group.Members.Clear();
        
        while (hasMoreMembers)
        {
            int endRange = startRange + step - 1;

            // Format the range attribute query (e.g., "member;range=0-1499")
            string memberAttributeWithRange = $"member;range={startRange}-{endRange}";

            var request = new SearchRequest(
                                            group.DistinguishedName,
                                            "(objectClass=*)",
                                            SearchScope.Base, // Base scope targets only this specific group object
                                            new string[]
                                            {
                                                memberAttributeWithRange
                                            }
                                           );

            var response = (SearchResponse)_ldapConnection.SendRequest(request);

            if (response.Entries.Count == 0)
                break;

            SearchResultEntry groupEntry           = response.Entries[0];
            bool              rangeFoundInThisLoop = false;
            

            foreach (string attrName in groupEntry.Attributes.AttributeNames)
            {
                // Active Directory will return either "member;range=X-Y" or "member;range=X-*"
                if (attrName.StartsWith("member;range=", StringComparison.OrdinalIgnoreCase))
                {
                    rangeFoundInThisLoop = true;
                    DirectoryAttribute attribute = groupEntry.Attributes[attrName];

                    // Extract Distinguished Names of the members
                    foreach (object val in attribute.GetValues(typeof(string)))
                    {
                        group.Members.Add(val.ToString());
                    }

                    // If the attribute name ends with "-*", we have reached the final block
                    if (attrName.EndsWith("-*"))
                    {
                        hasMoreMembers = false;
                    }
                    else
                    {
                        startRange += step;
                    }

                    break;
                }
            }

            // Fallback: If the group has < 1500 members, AD ignores ranges and returns a normal "member" attribute
            if (!rangeFoundInThisLoop)
            {
                if (groupEntry.Attributes.Contains("member"))
                {
                    DirectoryAttribute attribute = groupEntry.Attributes["member"];
                    foreach (object val in attribute.GetValues(typeof(string)))
                    {
                        group.Members.Add(val.ToString());
                    }
                }

                hasMoreMembers = false;
            }
        }

        return Result.Ok();
    }


    /// <summary>
    /// After saving the group object, this method checks for any new members to add or existing members to remove from the group.
    /// It processes these changes by sending appropriate LDAP requests to modify the group's membership in Active Directory.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    protected override Result AfterSave(ADpGroup obj)
    {
        // If the object has members that were added or removed, process them here.
        if (obj.NewMembers.Count > 0)
        {
            try
            {
                DirectoryAttributeModification memberModification = new DirectoryAttributeModification
                {
                    Name      = "member",
                    Operation = DirectoryAttributeOperation.Add
                };

                foreach (string member in obj.NewMembers)
                {
                    memberModification.Add(member);
                }


                // Add member to group
                var request = new ModifyRequest(obj.DistinguishedName);

                request.Modifications.Add(memberModification);

                ModifyResponse responmse = (ModifyResponse)_ldapConnection.SendRequest(request);
                if (responmse.ResultCode == ResultCode.Success)
                {
                    obj.Members.AddRange(obj.NewMembers);
                    obj.NewMembers.Clear();
                    return Result.Ok();
                }

                // Failure.  
                return Result.Fail(responmse.ErrorMessage);
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }

        return Result.Ok();

    }

    /// <summary>
    /// Retrieves all child groups under the specified parent distinguished name (DN) in Active Directory.  This method performs a one-level search to find all groups that are direct children of the given parent DN.
    /// </summary>
    /// <param name="parentDn"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public List<ADpGroup> GetAllChildGroups(ADSPath parentDn,SearchScope searchScope = SearchScope.OneLevel)
    {
        string                searchFilter = $"(&(objectClass={ADpCommon.OBJ_CLASS_GROUP}))";
        Result<List<ADpGroup>> result       = Find(parentDn.Path, searchScope, searchFilter);

        if (result.IsSuccess)
            return result.Value;

        if (result.ErrorTitle == "NotFound")
            return new List<ADpGroup>();
        
        throw new Exception($"Failed to retrieve child groups under {parentDn.Path}. Error: {result.Errors[0].Message}");
    }
}
