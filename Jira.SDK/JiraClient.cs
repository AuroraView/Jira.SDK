﻿using System.Dynamic;
using System.Net.Cache;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp.Extensions;
using Jira.SDK.Domain;
using System.Reflection;

namespace Jira.SDK
{
	public class JiraClient : IJiraClient
	{
		public enum JiraObjectEnum
		{
			Fields,
			Projects,
			Project,
			AssignableUser,
			ProjectVersions,
			Issue,
			Issues,
			Worklog,
			User,
			AgileBoards,
			Sprints,
			BacklogSprints,
			Sprint,
			SprintIssues,
            Filters
		}

		private RestClient Client { get; set; }

		private const String JiraAPIServiceURI = "/rest/api/latest";
		private const String JiraAgileServiceURI = "/rest/greenhopper/latest";

		private Dictionary<JiraObjectEnum, String> _methods = new Dictionary<JiraObjectEnum, String>()
        {
			{JiraObjectEnum.Fields, String.Format("{0}/field/", JiraAPIServiceURI)},
            {JiraObjectEnum.Projects, String.Format("{0}/project/", JiraAPIServiceURI)},
            {JiraObjectEnum.Project, String.Format("{0}/project/{{projectKey}}/", JiraAPIServiceURI)},
            {JiraObjectEnum.ProjectVersions, String.Format("{0}/project/{{projectKey}}/versions/", JiraAPIServiceURI)},
            {JiraObjectEnum.AssignableUser, String.Format("{0}/user/assignable/search/", JiraAPIServiceURI)},
            {JiraObjectEnum.Issue, String.Format("{0}/issue/{{issueKey}}/", JiraAPIServiceURI)},
            {JiraObjectEnum.Issues, String.Format("{0}/search/", JiraAPIServiceURI)},
            {JiraObjectEnum.Worklog, String.Format("{0}/issue/{{issueKey}}/worklog/", JiraAPIServiceURI)},
			{JiraObjectEnum.User, String.Format("{0}/user/", JiraAPIServiceURI)},
            {JiraObjectEnum.Filters, String.Format("{0}/filter/favourite", JiraAPIServiceURI)},
			{JiraObjectEnum.AgileBoards, String.Format("{0}/rapidviews/list/", JiraAgileServiceURI)},
			{JiraObjectEnum.Sprints, String.Format("{0}/sprintquery/{{boardID}}/", JiraAgileServiceURI)},
			{JiraObjectEnum.BacklogSprints, String.Format("{0}/xboard/plan/backlog/data.json", JiraAgileServiceURI)},
			{JiraObjectEnum.Sprint, String.Format("{0}/rapid/charts/sprintreport/", JiraAgileServiceURI)},
			{JiraObjectEnum.SprintIssues, String.Format("{0}/sprintquery/", JiraAgileServiceURI)}
        };

		public JiraClient(RestClient client)
		{
			Client = client;
		}

		public JiraClient(String url, String username, String password)
		{
			Client = new RestClient(url)
			{
				Authenticator = new HttpBasicAuthenticator(username, password)
			};
		}

		public List<Issue> SearchIssues(String jql)
		{
			return GetItem<IssueSearchResult>(JiraObjectEnum.Issues, new Dictionary<String, String>() { { "jql", jql }, { "maxResults", "700" } }).Issues;
		}

		#region Fields
		public List<Field> GetFields()
		{
			return GetList<Field>(JiraObjectEnum.Fields);
		}
		#endregion

		#region Projects
		public List<Project> GetProjects()
		{
			return GetList<Project>(JiraObjectEnum.Projects);
		}

		public Project GetProject(String projectKey)
		{
			return GetList<Project>(JiraObjectEnum.Project, keys: new Dictionary<string, string>() { { "projectKey", projectKey } }).FirstOrDefault();
		}
		#endregion

		#region Project versions
		public List<ProjectVersion> GetProjectVersions(String projectKey)
		{
			return GetList<ProjectVersion>(JiraObjectEnum.ProjectVersions,
							   keys: new Dictionary<string, string>() { { "projectKey", projectKey } });
		}
		#endregion

		#region Users
		public User GetUser(String username)
		{
			return GetItem<User>(JiraObjectEnum.User, new Dictionary<string, string>() { { "username", username } });
		}

		public List<User> GetAssignableUsers(String projectKey)
		{
			return GetList<User>(JiraObjectEnum.AssignableUser,
							   parameters: new Dictionary<string, string>() { { "project", projectKey } });
		}
		#endregion

		#region Agile boards
		public List<AgileBoard> GetAgileBoards()
		{
			return GetItem<AgileBoardView>(JiraObjectEnum.AgileBoards).Views;
		}

		public List<Sprint> GetSprintsFromAgileBoard(Int32 agileBoardID)
		{
			return GetItem<SprintResult>(JiraObjectEnum.Sprints, keys: new Dictionary<String, String>() { { "boardID", agileBoardID.ToString() } }).Sprints;
		}

		public List<Sprint> GetBacklogSprintsFromAgileBoard(Int32 agileBoardID)
		{
			return GetItem<SprintResult>(JiraObjectEnum.BacklogSprints, parameters: new Dictionary<String, String>() { { "rapidViewId", agileBoardID.ToString() } }).Sprints;
		}

		public Sprint GetSprint(Int32 agileBoardID, Int32 sprintID)
		{
			return GetItem<SprintResult>(JiraObjectEnum.Sprint, parameters: new Dictionary<String, String>() { { "rapidViewId", agileBoardID.ToString() }, { "sprintId", sprintID.ToString() } }).Sprint;
		}

		public List<Issue> GetIssuesFromSprint(int sprintID)
		{
			return SearchIssues(String.Format("Sprint = {0}", sprintID));
		}
		#endregion

		#region Issues
		public Issue GetIssue(String key)
		{
			return GetItem<Issue>(JiraObjectEnum.Issue, keys: new Dictionary<String, String>() { { "issueKey", key } });
		}

		public List<Issue> GetSubtasksFromIssue(String issueKey)
		{
			return SearchIssues(String.Format("parent=\"{0}\"", issueKey));
		}

		public List<Issue> GetIssuesFromProjectVersion(String projectKey, String projectVersionName)
		{
			return SearchIssues(String.Format("project=\"{0}\"&fixversion=\"{1}\"",
							projectKey, projectVersionName));
		}

		public Dictionary<String, String> GetIssueCustomFieldsFromIssue(String key)
		{
			dynamic obj = ExecuteDynamic(JiraObjectEnum.Issue, keys: new Dictionary<String, String>() { { "issueKey", key } });

			List<PropertyInfo> customFieldProperties = ((Type)(obj.fields).GetType()).GetProperties().Where(prop => prop.Name.StartsWith("customfield")).ToList();

			Dictionary<String, String> customfields = new Dictionary<String, String>();
			foreach (PropertyInfo property in customFieldProperties)
			{
				property.GetValue(obj.Fields);
			}

			return customfields;
		}

        public List<IssueFilter> GetFavoriteFilters()
        {
            return GetList<IssueFilter>(JiraObjectEnum.Filters);
        }

		public List<Issue> GetIssuesWithEpicLink(String epicLink)
		{
			return SearchIssues(String.Format("'Epic Link' = {0}", epicLink));
		}

		public List<Issue> GetEpicIssuesFromProject(String projectName)
		{
			return SearchIssues(String.Format("project = '{0}' AND Type = Epic", projectName));
		}

		public Issue GetEpicIssueFromProject(String projectName, String epicName)
		{
			return SearchIssues(String.Format("project = '{0}' AND Type = Epic and 'Epic name' = '{1}'", projectName, epicName)).FirstOrDefault();
		}
		#endregion

		#region Worklog
		public WorklogSearchResult GetWorkLogs(String issueKey)
		{
			return GetItem<WorklogSearchResult>(JiraObjectEnum.Worklog,
					   keys: new Dictionary<String, String>() { { "issueKey", issueKey } });
		}
		#endregion

		#region Jira communication
		private T GetItem<T>(JiraObjectEnum objectType, Dictionary<String, String> parameters = null,
			Dictionary<String, String> keys = null) where T : new()
		{
			return Execute<T>(objectType, parameters, keys);
		}

		private List<T> GetList<T>(JiraObjectEnum objectType, Dictionary<String, String> parameters = null, Dictionary<String, String> keys = null) where T : new()
		{
			return Execute<List<T>>(objectType, parameters, keys);
		}

		private T Execute<T>(JiraObjectEnum objectType, Dictionary<String, String> parameters = null, Dictionary<String, String> keys = null) where T : new()
		{
			IRestResponse<T> response = Client.Execute<T>(GetRequest(objectType, parameters ?? new Dictionary<String, String>(), keys ?? new Dictionary<String, String>()));

			if (response.ErrorException != null)
			{
				throw response.ErrorException;
			}
			if (response.ResponseStatus != ResponseStatus.Completed)
			{
				throw new Exception(response.ErrorMessage);
			}

			return response.Data;
		}

		private dynamic ExecuteDynamic(JiraObjectEnum objectType, Dictionary<String, String> parameters = null, Dictionary<String, String> keys = null)
		{
			IRestResponse<dynamic> response = Client.ExecuteDynamic(GetRequest(objectType, parameters ?? new Dictionary<String, String>(), keys ?? new Dictionary<String, String>()));

			if (response.ErrorException != null)
			{
				throw response.ErrorException;
			}
			if (response.ResponseStatus != ResponseStatus.Completed)
			{
				throw new Exception(response.ErrorMessage);
			}

			return response.Data;
		}

		public RestRequest GetRequest(JiraObjectEnum objectType, Dictionary<String, String> parameters,
			Dictionary<String, String> keys)
		{
			if (!_methods.ContainsKey(objectType))
				throw new NotImplementedException();

			RestRequest request = new RestRequest(_methods[objectType], Method.GET)
			{
				RequestFormat = DataFormat.Json,
				OnBeforeDeserialization = resp => resp.ContentType = "application/json"
			};

			foreach (KeyValuePair<String, String> key in keys)
			{
				request.AddParameter(key.Key, key.Value, ParameterType.UrlSegment);
			}

			foreach (KeyValuePair<String, String> parameter in parameters)
			{
				request.AddParameter(parameter.Key, parameter.Value);
			}

			return request;
		}
		#endregion
	};
}
