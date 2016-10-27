using GogsKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GogsActiveDirectorySync
{
    public class GroupSynchronizer
    {
        private readonly AppConfiguration appConfiguration;

        private readonly GogsClient gogsClient;
        private readonly ActiveDirectoryClient activeDirectoryClient;
        private readonly ISet<string> excludedUsernames;

        public GroupSynchronizer(AppConfiguration appConfiguration)
        {
            if (appConfiguration == null) throw new ArgumentNullException(nameof(appConfiguration));
            this.appConfiguration = appConfiguration;

            this.gogsClient = CreateGogsClient(appConfiguration);
            this.activeDirectoryClient = new ActiveDirectoryClient(appConfiguration.ActiveDirectoryGroupContainer);
            this.excludedUsernames = new SortedSet<string>(appConfiguration.ExcludedUsernames?.Select(x => x.ToLower()) ?? Array.Empty<string>());
        }

        private static GogsClient CreateGogsClient(AppConfiguration appConfiguration)
        {
            Credentials gogsCredentials;
            if (appConfiguration.GogsAccessToken != null)
            {
                gogsCredentials = new Credentials(appConfiguration.GogsAccessToken);
            }
            else if (appConfiguration.GogsUsername != null)
            {
                gogsCredentials = new Credentials(appConfiguration.GogsUsername, appConfiguration.GogsPassword);
            }
            else
            {
                gogsCredentials = Credentials.Anonymous;
            }
            return new GogsClient(appConfiguration.GogsApiUri, gogsCredentials);
        }

        public async Task SynchronizeAsync(IProgress<string> progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var gogsUserCache = new Dictionary<string, GogsKit.UserResult>();
            foreach (var groupMapping in appConfiguration.GroupMappings)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var gogsTeam = await GetOrCreateGogsOrgAndTeam(groupMapping, progress, cancellationToken);
                if (gogsTeam == null)
                {
                    progress?.Report($"No Gogs team found for AD group \"{groupMapping.ActiveDirectoryName}\"");
                    continue;
                }

                var adUsers = activeDirectoryClient.GetUsers(groupMapping.ActiveDirectoryName).ToArray();
                var gogsUsers = new List<GogsKit.UserResult>(adUsers.Length);
                foreach (var adUser in adUsers)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (excludedUsernames != null && excludedUsernames.Contains(adUser.Username, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (!string.IsNullOrWhiteSpace(appConfiguration.RequiredActiveDirectoryGroupName))
                    {
                        var requiredAdGroupName = appConfiguration.RequiredActiveDirectoryGroupName;
                        if (!adUser.GroupNames.Any(groupName => string.Equals(groupName, requiredAdGroupName, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }
                    }

                    GogsKit.UserResult gogsUser;
                    if (!gogsUserCache.TryGetValue(adUser.Username, out gogsUser))
                    {
                        gogsUser = await GetOrCreateGogsUser(adUser, progress, cancellationToken);
                        if (gogsUser != null)
                        {
                            gogsUserCache[adUser.Username] = gogsUser;
                        }
                    }

                    if (gogsUser != null)
                    {
                        gogsUsers.Add(gogsUser);
                    }
                    else
                    {
                        progress?.Report($"No Gogs user found for AD username \"{adUser.Username}\"");
                    }
                }

                foreach (var gogsUser in gogsUsers)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress?.Report($"Adding Gogs user \"{gogsUser.Username}\" to org \"{groupMapping.GogsOrgName}\" on team \"{gogsTeam.Name}\"");
                    if (!appConfiguration.IsDryRun)
                    {
                        try
                        {
                            await gogsClient.Admin.AddTeamMember(gogsTeam.Id, gogsUser.Username, cancellationToken: cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            NLog.LogManager.GetCurrentClassLogger().Error(ex, $"Failed to add Gogs user \"{gogsUser.Username}\" to org \"{groupMapping.GogsOrgName}\" on team \"{gogsTeam.Name}\"");
                        }
                    }
                }
            }
        }

        private async Task<GogsKit.TeamResult> GetOrCreateGogsOrgAndTeam(GroupMapping groupMapping, IProgress<string> progress, CancellationToken cancellationToken)
        {
            var adGroupName = groupMapping.ActiveDirectoryName;
            var gogsOrgName = groupMapping.GogsOrgName;
            if (string.IsNullOrWhiteSpace(adGroupName) || string.IsNullOrWhiteSpace(gogsOrgName))
            {
                return null;
            }

            GogsKit.OrganizationResult gogsOrganization;
            try
            {
                gogsOrganization = await gogsClient.Orgs.GetAsync(gogsOrgName, cancellationToken: cancellationToken);
            }
            catch (GogsKit.Exceptions.GogsKitNotFoundException)
            {
                gogsOrganization = null;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, $"Failed to get Gogs org \"{gogsOrgName}\"");
                gogsOrganization = null;
            }

            if (gogsOrganization == null && appConfiguration.EnableGogsOrgCreation)
            {
                progress?.Report($"Creating missing Gogs org \"{gogsOrgName}\"");
                if (!appConfiguration.IsDryRun)
                {
                    try
                    {
                        gogsOrganization = await gogsClient.Admin.CreateOrg(appConfiguration.GogsUsername, new CreateOrgOption
                        {
                            UserName = gogsOrgName,
                            FullName = adGroupName,
                        }, cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        NLog.LogManager.GetCurrentClassLogger().Error(ex, $"Failed to create Gogs org \"{gogsOrgName}\"");
                        gogsOrganization = null;
                    }
                }
            }

            // If we don't have an organization, then there's no point in looking for a team.
            if (gogsOrganization == null)
            {
                return null;
            }

            const string defaultGogsTeamName = "ActiveDirectory";
            var gogsTeamName = groupMapping.GogsTeamName ?? defaultGogsTeamName;
            IReadOnlyCollection<GogsKit.TeamResult> orgTeams;
            try
            {
                orgTeams = await gogsClient.Orgs.GetTeamsAsync(gogsOrgName, cancellationToken: cancellationToken);
            }
            catch (GogsKit.Exceptions.GogsKitNotFoundException)
            {
                //NLog.LogManager.GetCurrentClassLogger().Warn($"No Gogs teams found for org \"{gogsOrgName}\"");
                orgTeams = Array.Empty<GogsKit.TeamResult>();
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, $"Failed to get Gogs teams for org \"{gogsOrgName}\"");
                orgTeams = Array.Empty<GogsKit.TeamResult>();
            }

            var gogsOrgSyncTeam = orgTeams.FirstOrDefault(t => string.Equals(t.Name, gogsTeamName));
            if (gogsOrgSyncTeam == null && appConfiguration.EnableGogsTeamCreation)
            {
                progress?.Report($"Creating missing Gogs team \"{gogsTeamName}\" for org \"{gogsOrgName}\"");
                if (!appConfiguration.IsDryRun)
                {
                    try
                    {
                        gogsOrgSyncTeam = await gogsClient.Admin.CreateTeam(gogsOrgName, new CreateTeamOption
                        {
                            Name = gogsTeamName,
                            Description = "Members of this team are automatically synchronized with Active Directory",
                            Permission = "write"
                        }, cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        NLog.LogManager.GetCurrentClassLogger().Error(ex, $"Failed to create Gogs team \"{gogsTeamName}\" for org \"{gogsOrgName}\"");
                        gogsOrgSyncTeam = null;
                    }
                }
            }
            return gogsOrgSyncTeam;
        }

        private async Task<GogsKit.UserResult> GetOrCreateGogsUser(ActiveDirectoryUser adUser, IProgress<string> progress, CancellationToken cancellationToken)
        {
            GogsKit.UserResult gogsUser;
            try
            {
                gogsUser = await gogsClient.Users.GetAsync(adUser.Username, cancellationToken: cancellationToken);
            }
            catch (GogsKit.Exceptions.GogsKitNotFoundException)
            {
                gogsUser = null;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, $"Failed to get Gogs user \"{adUser.Username}\"");
                gogsUser = null;
            }

            if (gogsUser == null && appConfiguration.EnableGogsUserCreation)
            {
                progress?.Report($"Creating missing Gogs user \"{adUser.Username}\" (\"{adUser.ActiveDirectoryName}\")");
                if (!appConfiguration.IsDryRun)
                {
                    try
                    {
                        gogsUser = await gogsClient.Admin.CreateUserAsync(new CreateUserOption
                        {
                            SourceId = appConfiguration.GogsLdapAuthenticationSourceId,
                            Username = adUser.Username,
                            FullName = adUser.DisplayName,
                            Email = adUser.EmailAddress,
                            SendNotify = false,
                        }, cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        NLog.LogManager.GetCurrentClassLogger().Error(ex, $"Failed to create Gogs user \"{adUser.Username}\"");
                        gogsUser = null;
                    }
                }
            }

            return gogsUser;
        }
    }
}
