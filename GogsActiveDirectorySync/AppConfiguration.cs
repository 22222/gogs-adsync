using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace GogsActiveDirectorySync
{
    public class AppConfiguration
    {
        /// <summary>
        /// The amount of time to wait between sync operations.
        /// </summary>
        public TimeSpan SyncInterval { get; set; }

        /// <summary>
        /// The minimum time of day that the sync operation is allowed to run at.
        /// This can be greater than the <see cref="MaximumTimeOfDay"/> for an 
        /// allowed time period that crosses midnight (like 17:00 to 4:00).
        /// </summary>
        public TimeSpan? MinimumTimeOfDay { get; set; }

        /// <summary>
        /// The maximum time of day that the sync operation is allowed to run at.
        /// </summary>
        public TimeSpan? MaximumTimeOfDay { get; set; }

        /// <summary>
        /// The container to use as the root of the Active Directory context. 
        /// </summary>
        public string ActiveDirectoryGroupContainer { get; set; }

        /// <summary>
        /// An (optional) Active Directory group that's required for all users included in the sync.
        /// If the user isn't in this AD group, then no Gogs user will be created for them and they will 
        /// not be placed into any Gogs organizations/teams.
        /// </summary>
        public string RequiredActiveDirectoryGroupName { get; set; }

        /// <summary>
        /// The URI to the Gogs API.  Example: https://try.gogs.io/api/v1/
        /// </summary>
        public Uri GogsApiUri { get; set; }

        /// <summary>
        /// The Gogs username of an admin user.  
        /// This can be used for authentication, but it's also used as the initial owner of any newly created groups.
        /// </summary>
        public string GogsUsername { get; set; }

        /// <summary>
        /// The password for the <see cref="GogsUsername"/> (if using password-based authentication).
        /// </summary>
        public string GogsPassword { get; set; }

        /// <summary>
        /// The Gogs access token (if using instead of password authentication).
        /// </summary>
        public string GogsAccessToken { get; set; }

        /// <summary>
        /// The Gogs authentication source for LDAP authentication.  
        /// Probably "1" if you only have one authentication source.
        /// </summary>
        public int GogsLdapAuthenticationSourceId { get; set; }

        /// <summary>
        /// Set this to true to have a Gogs access token will be created for your user if you don't already have one.
        /// </summary>
        public bool EnableGogsAccessTokenGeneration { get; set; }

        /// <summary>
        /// If true, users will be created in Gogs for all users 
        /// in the <see cref="ActiveDirectoryGroupName"/> group 
        /// (except those in the <see cref="ExcludedUsernames"/>).
        /// </summary>
        public bool EnableGogsUserCreation { get; set; }

        /// <summary>
        /// If true, organizations will be created in Gogs for all groups with an
        /// Active Directory match in <see cref="GroupMappings"/>.
        /// </summary>
        public bool EnableGogsOrgCreation { get; set; }

        /// <summary>
        /// Enables team creation in Gogs.
        /// If your version of Gogs has a bug that prevents the API from creating teams, 
        /// then teams won't be created even if this is enabled.
        /// </summary>
        public bool EnableGogsTeamCreation { get; set; }

        /// <summary>
        /// If true, nothing will actually be created.
        /// Use this to simulate a run without actually doing anything.
        /// </summary>
        public bool IsDryRun { get; set; }

        /// <summary>
        /// The mappings from ActiveDirectory groups to Gogs organizations and teams.
        /// </summary>
        public IReadOnlyCollection<GroupMapping> GroupMappings { get; set; }

        /// <summary>
        /// Any usernames that should be excluded from Gogs.  
        /// This affects both creating users and assigning users to groups.
        /// </summary>
        public IReadOnlyCollection<string> ExcludedUsernames { get; set; }

        public void LoadConfigFile()
        {
            TimeSpan syncInterval;
            if (!TimeSpan.TryParse(ConfigurationManager.AppSettings["SyncInterval"], out syncInterval))
            {
                int syncIntervalHours;
                if (int.TryParse(ConfigurationManager.AppSettings["SyncIntervalHours"], out syncIntervalHours))
                {
                    syncInterval = TimeSpan.FromHours(syncIntervalHours);
                }
                else
                {
                    int syncIntervalMinutes;
                    if (int.TryParse(ConfigurationManager.AppSettings["SyncIntervalMinutes"], out syncIntervalMinutes))
                    {
                        syncInterval = TimeSpan.FromMinutes(syncIntervalMinutes);
                    }
                    else
                    {
                        syncInterval = TimeSpan.FromHours(23);
                    }
                }
            }
            SyncInterval = syncInterval;

            TimeSpan maximumTimeOfDay;
            if (TimeSpan.TryParse(ConfigurationManager.AppSettings["MaximumTimeOfDay"], out maximumTimeOfDay))
            {
                MaximumTimeOfDay = maximumTimeOfDay;
            }

            TimeSpan minimumTimeOfDay;
            if (TimeSpan.TryParse(ConfigurationManager.AppSettings["MinimumTimeOfDay"], out minimumTimeOfDay))
            {
                MinimumTimeOfDay = minimumTimeOfDay;
            }

            ActiveDirectoryGroupContainer = ConfigurationManager.AppSettings["ActiveDirectoryGroupContainer"];
            RequiredActiveDirectoryGroupName = ConfigurationManager.AppSettings["RequiredActiveDirectoryGroupName"];

            var gogsApiUrl = ConfigurationManager.AppSettings["GogsApiUrl"];
            GogsApiUri = gogsApiUrl != null ? new Uri(gogsApiUrl) : null;
            GogsUsername = ConfigurationManager.AppSettings["GogsUsername"];
            GogsPassword = ConfigurationManager.AppSettings["GogsPassword"];
            GogsAccessToken = ConfigurationManager.AppSettings["GogsAccessToken"];

            int sourceId;
            if (!int.TryParse(ConfigurationManager.AppSettings["GogsLdapAuthenticationSourceId"], out sourceId))
            {
                sourceId = 0;
            }
            GogsLdapAuthenticationSourceId = sourceId;

            bool enableAccessTokenGeneration;
            if (!bool.TryParse(ConfigurationManager.AppSettings["EnableGogsAccessTokenGeneration"], out enableAccessTokenGeneration))
            {
                enableAccessTokenGeneration = false;
            }
            EnableGogsAccessTokenGeneration = enableAccessTokenGeneration;

            bool enableGogsUserCreation;
            if (!bool.TryParse(ConfigurationManager.AppSettings["EnableGogsUserCreation"], out enableGogsUserCreation))
            {
                enableGogsUserCreation = false;
            }
            EnableGogsUserCreation = enableGogsUserCreation;

            bool enableGogsOrgCreation;
            if (!bool.TryParse(ConfigurationManager.AppSettings["EnableGogsOrgCreation"], out enableGogsOrgCreation))
            {
                enableGogsOrgCreation = false;
            }
            EnableGogsOrgCreation = enableGogsOrgCreation;

            bool enableGogsTeamCreation;
            if (!bool.TryParse(ConfigurationManager.AppSettings["EnableGogsTeamCreation"], out enableGogsTeamCreation))
            {
                enableGogsTeamCreation = false;
            }
            EnableGogsTeamCreation = enableGogsTeamCreation;

            bool isDryRun;
            if (!bool.TryParse(ConfigurationManager.AppSettings["IsDryRun"], out isDryRun))
            {
                isDryRun = !string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["IsDryRun"]);
            }
            IsDryRun = isDryRun;

            var groupMappingSection = ConfigurationManager.GetSection("groupNameMappings") as GroupMappingConfiguration;
            GroupMappings = groupMappingSection?.GroupMappings;

            var excludedUsernamesCsv = ConfigurationManager.AppSettings["ExcludedUsernamesCsv"]
                ?? ConfigurationManager.AppSettings["ExcludedUsernames"]
                ?? ConfigurationManager.AppSettings["ExcludedUsername"];
            ExcludedUsernames = excludedUsernamesCsv?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    public class GroupMapping
    {
        public GroupMapping(string activeDirectoryName, string gogsOrgName, string gogsTeamName = null)
        {
            if (activeDirectoryName == null) throw new ArgumentNullException(nameof(activeDirectoryName));
            if (gogsOrgName == null) throw new ArgumentNullException(nameof(gogsOrgName));

            this.ActiveDirectoryName = activeDirectoryName;
            this.GogsOrgName = gogsOrgName;
            this.GogsTeamName = gogsTeamName;
        }

        public string ActiveDirectoryName { get; }
        public string GogsOrgName { get; }
        public string GogsTeamName { get; }
    }

    public class GroupMappingConfiguration : ConfigurationSection
    {
        public IReadOnlyCollection<GroupMapping> GroupMappings { get; set; } = Array.Empty<GroupMapping>();

        protected override void DeserializeSection(XmlReader reader)
        {
            var doc = XDocument.Load(reader);
            GroupMappings = ParseMappingElements(doc).ToArray();
        }

        private IEnumerable<GroupMapping> ParseMappingElements(XDocument doc)
        {
            var mappingElements = doc.Root.Elements("mapping");
            foreach (var mappingElement in mappingElements)
            {
                var activeDirectoryName = mappingElement.Attribute("activeDirectoryName")?.Value;
                var gogsOrganizationName = mappingElement.Attribute("gogsOrgName")?.Value
                    ?? mappingElement.Attribute("gogsOrganizationName")?.Value
                    ?? mappingElement.Attribute("gogsName")?.Value;
                var gogsTeamName = mappingElement.Attribute("gogsTeamName")?.Value;
                yield return new GroupMapping(activeDirectoryName, gogsOrganizationName, gogsTeamName: gogsTeamName);
            }
        }
    }
}
