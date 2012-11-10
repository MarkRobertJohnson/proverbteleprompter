using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace ProverbTeleprompter.WebController
{
	[ServiceContract]
	public interface IPtController
	{
		[OperationContract]
		[WebGet(BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, UriTemplate = "{*contentPath}")]
		Stream LoadContent(string contentPath);

		[OperationContract]
		[WebGet(BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, UriTemplate = "api/{command}/{value}")]
		Stream SendCommand(string command, string value);
	}
}
