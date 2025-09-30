namespace EpicMorg.Atlassian.Downloader.Models;
using System.Collections.Generic;

internal static class SourceInformation
{
    public static IReadOnlyList<string> AtlassianSources { get; } = new[] {

            //official links
            "https://my.atlassian.com/download/feeds/archived/bamboo.json",
            "https://my.atlassian.com/download/feeds/archived/clover.json",
            "https://my.atlassian.com/download/feeds/archived/confluence.json",
            "https://my.atlassian.com/download/feeds/archived/crowd.json",
            "https://my.atlassian.com/download/feeds/archived/crucible.json",
            "https://my.atlassian.com/download/feeds/archived/fisheye.json",
            "https://my.atlassian.com/download/feeds/archived/jira-core.json",
            "https://my.atlassian.com/download/feeds/archived/jira-servicedesk.json",
            "https://my.atlassian.com/download/feeds/archived/jira-software.json",
            "https://my.atlassian.com/download/feeds/archived/jira.json",
            "https://my.atlassian.com/download/feeds/archived/stash.json",
            "https://my.atlassian.com/download/feeds/archived/mesh.json",

            //cdn mirror of official links
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/archived/bamboo.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/archived/clover.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/archived/confluence.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/archived/crowd.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/archived/crucible.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/archived/fisheye.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/archived/jira-core.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/archived/jira-servicedesk.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/archived/jira-software.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/archived/jira.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/archived/stash.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/archived/mesh.json",

            //official links
            "https://my.atlassian.com/download/feeds/current/bamboo.json",
            "https://my.atlassian.com/download/feeds/current/clover.json",
            "https://my.atlassian.com/download/feeds/current/confluence.json",
            "https://my.atlassian.com/download/feeds/current/crowd.json",
            "https://my.atlassian.com/download/feeds/current/crucible.json",
            "https://my.atlassian.com/download/feeds/current/fisheye.json",
            "https://my.atlassian.com/download/feeds/current/jira-core.json",
            "https://my.atlassian.com/download/feeds/current/jira-servicedesk.json",
            "https://my.atlassian.com/download/feeds/current/jira-software.json",
            "https://my.atlassian.com/download/feeds/current/stash.json",
            "https://my.atlassian.com/download/feeds/current/mesh.json",

            //cdn mirror of official links
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/current/bamboo.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/current/clover.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/current/confluence.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/current/crowd.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/current/crucible.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/current/fisheye.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/current/jira-core.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/current/jira-servicedesk.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/current/jira-software.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/current/stash.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/current/mesh.json",

            //official links
            "https://my.atlassian.com/download/feeds/eap/bamboo.json",
            "https://my.atlassian.com/download/feeds/eap/confluence.json",
            "https://my.atlassian.com/download/feeds/eap/jira.json",
            "https://my.atlassian.com/download/feeds/eap/jira-servicedesk.json",
            "https://my.atlassian.com/download/feeds/eap/stash.json",
            //"https://my.atlassian.com/download/feeds/eap/mesh.json", //404

            //cdn mirror of official links
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/eap/bamboo.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/eap/confluence.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/eap/jira.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/eap/jira-servicedesk.json",
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/eap/stash.json",
            //"https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/eap/mesh.json",  //404
            
            //https://raw.githubusercontent.com/EpicMorg/atlassian-json/master/json-backups/archived/sourcetree.json //unstable link with r\l
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/archived/sourcetree.json",
            
            //https://raw.githubusercontent.com/EpicMorg/atlassian-json/master/json-backups/current/sourcetree.json //unstable link with r\l
            "https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/current/sourcetree.json"

        };
}
