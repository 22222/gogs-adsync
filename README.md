A windows service for synchronizing Active Directory users and groups with [Gogs](https://gogs.io/).

[![Build status](https://ci.appveyor.com/api/projects/status/bfw9yd8a49sdbrc2?svg=true)](https://ci.appveyor.com/project/22222/gogs-adsync)

Install
=======
Download the latest release.  Then you can run `GogsActiveDirectorySync.exe` directly as a console application, or install it as a windows service:

```
GogsActiveDirectorySync.exe install –autostart
```

The [Topshelf](http://topshelf-project.com/) library is used to make this run as a windows service.  So see the [Topshelf commandline documentation](http://docs.topshelf-project.com/en/latest/overview/commandline.html) for more details.


Configuration
=============
The configuration for this thing is in [GogsActiveDirectorySync.exe.config](GogsActiveDirectorySync/App.config).  There's also NLog configuration in the [NLog.config](GogsActiveDirectorySync/NLog.config).

Some AppSettings you'll probably want to change:

* `SyncIntervalHours`: How often the sync runs
* `MinimumTimeOfDay`, `MaximumTimeOfDay`: Can be used to make sure the sync only runs at night (or any interval you want)
* `ActiveDirectoryGroupContainer`: The base container for all of your Active Directory groups?  You might be able to leave this blank.
* `GogsApiUrl`: The URL to your Gogs installation's API (like `https://try.gogs.io/api/v1/`)
* `GogsUsername`: An Gogs user with enough permissions to create users and organizations
* `GogsPassword`: Set this if you want to use password authentication with the Gogs API
* `GogsAccessToken`: Set this if you want to use token authentication with the Gogs API

There's more of these, but I'm tired of writing about them in this readme that no one will read.  There may be more comments about them in [AppConfiguration.cs](GogsActiveDirectorySync/AppConfiguration.cs).

Outside of those AppSettings, there's also a custom `groupNameMappings` section.  This determines how things will be mapped from Active Directory to Gogs.  Example:

```xml
<groupNameMappings>
	<mapping activeDirectoryName="Development" gogsOrgName="Dev" gogsTeamName="Active Directory" />
</groupNameMappings>
```

You need an entry in there for every Active Directory group you want to have synchronized with a Gogs organization.