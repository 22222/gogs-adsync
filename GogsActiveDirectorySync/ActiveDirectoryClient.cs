using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GogsActiveDirectorySync
{
    public class ActiveDirectoryClient
    {
        private readonly string groupContainer;

        public ActiveDirectoryClient(string groupContainer)
        {
            this.groupContainer = groupContainer;
        }

        public IEnumerable<ActiveDirectoryUser> GetUsers(string groupName)
        {
            var domain = new PrincipalContext(ContextType.Domain, Environment.UserDomainName, groupContainer);
            var groupQueryFilter = new GroupPrincipal(domain, groupName);
            var groupSearcher = new PrincipalSearcher(groupQueryFilter);
            var group = groupSearcher.FindOne() as GroupPrincipal;
            if (group == null)
            {
                //throw new InvalidOperationException($"Group \"{groupName}\" not found in container \"{groupContainer}\"");
                yield break;
            }

            var members = group.GetMembers().OfType<UserPrincipal>();
            foreach (var member in members)
            {
                var groups = member.GetGroups();
                var groupNames = new SortedSet<string>(groups.Select(g => g.Name));
                var result = new ActiveDirectoryUser()
                {
                    Username = member.SamAccountName,
                    DisplayName = member.DisplayName,
                    EmailAddress = member.EmailAddress,
                    ActiveDirectoryName = member.DistinguishedName,
                    GroupNames = groupNames,
                };
                yield return result;
            }
        }
    }

    public class ActiveDirectoryUser
    {
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string EmailAddress { get; set; }
        public string ActiveDirectoryName { get; set; }
        public IReadOnlyCollection<string> GroupNames { get; set; } = Array.Empty<string>();
    }
}
