using GogsKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GogsActiveDirectorySync
{
    public class GogsAccessTokenGenerator
    {
        private readonly GogsClient gogsClient;
        private readonly string gogsUsername;
        private readonly bool isDryRun;

        public GogsAccessTokenGenerator(AppConfiguration appConfiguration)
        {
            var gogsCredentials = new Credentials(appConfiguration.GogsUsername, appConfiguration.GogsPassword);
            this.gogsClient = new GogsClient(appConfiguration.GogsApiUri, gogsCredentials);
            this.gogsUsername = appConfiguration.GogsUsername;
            this.isDryRun = appConfiguration.IsDryRun;
        }

        public async Task<IReadOnlyCollection<GogsKit.TokenResult>> GetAccessTokensAsync()
        {
            return await gogsClient.Users.GetTokensAsync(gogsUsername) ?? Array.Empty<TokenResult>();
        }

        public async Task<IReadOnlyCollection<GogsKit.TokenResult>> CreateOrGetAccessTokensAsync()
        {
            var tokens = await GetAccessTokensAsync();
            if (tokens.Any() || isDryRun)
            {
                return tokens;
            }

            var token = await gogsClient.Users.CreateTokenAsync(gogsUsername, "Default");
            if (token == null)
            {
                return Array.Empty<TokenResult>();
            }
            return new[] { token };
        }
    }
}
