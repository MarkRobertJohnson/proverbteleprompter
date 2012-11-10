using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;

namespace ProverbTeleprompter.WebController
{
	[ServiceBehavior]
	public class PtController : IPtController
	{

		public static void Initialize()
		{

			var resourceAssembly = Assembly.GetExecutingAssembly();
			//Extract all web contant
			//Get only site resources
			string[] siteResources = resourceAssembly.GetManifestResourceNames().
				Where(x => x.StartsWith(typeof(PtController).Namespace)).ToArray();

			//Write resources to file system
			foreach (var siteResource in siteResources)
			{
				var e = resourceAssembly.GetManifestResourceInfo(siteResource);
				
				using (var s = resourceAssembly.GetManifestResourceStream(siteResource))
				{
					if (s is UnmanagedMemoryStream)
					{
						var strm = (s as UnmanagedMemoryStream);
						var buf = new byte[strm.Length];
						strm.Seek(0, SeekOrigin.End);
						strm.Seek(0, SeekOrigin.Begin);
						strm.Read(buf, 0, buf.Length);

						var outfile = Path.Combine(SiteBasePath, siteResource.Replace(typeof (PtController).Namespace + ".", ""));
						
						//Only overwrite if resource assembly is newer
						//if (!File.Exists(outfile) && 
					//		File.GetLastWriteTime(outfile) < File.GetLastWriteTime(resourceAssembly.Location))
							File.WriteAllBytes(outfile, buf);
						//contentDictionary.Add(resourceName, (T)(object)buf);
					}
				}
				
				//Select(y => y.Replace(typeof(PtController).Namespace + ".", ""))
				//Get folder path, create dir if not exists
			}
		}

		private Stream _outStream;

		[OperationBehavior(AutoDisposeParameters = false,ReleaseInstanceMode = ReleaseInstanceMode.None)]
		public Stream SendCommand(string command, string value)
		{
			if (WebOperationContext.Current != null)
			{
				WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.Expires, "Fri, 30 Jul 1970 06:47:23 GMT");
				WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.Connection, "Keep-Alive");
				WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentType, "text/plain");

			}
			if(command.ToLowerInvariant() == "scrolldownfaster")
			{
				MainWindow.SharedMainWindow.MainWindowViewModel.HandleKeyDown(Key.Down, false);
			}
			if (command.ToLowerInvariant() == "scrolldownfastercomplete")
			{
				MainWindow.SharedMainWindow.MainWindowViewModel.HandleKeyUp(Key.Down, false);
			}
			if (command.ToLowerInvariant() == "scrollupfaster")
			{
				MainWindow.SharedMainWindow.MainWindowViewModel.HandleKeyDown(Key.Up, false);
			}
			if (command.ToLowerInvariant() == "scrollupfastercomplete")
			{
				MainWindow.SharedMainWindow.MainWindowViewModel.HandleKeyUp(Key.Up, false);
			}

			
			_outStream = new MemoryStream();
			
			var data = Encoding.ASCII.GetBytes(new string('C', 20000));
		//	_outStream.Write(data, 0, data.Length);
			//var reader = new StreamReader(_outStream, Encoding.ASCII);
			var scriptPath = Path.Combine(SiteBasePath, "Site.Home.htm");
			var f = File.ReadAllBytes(scriptPath);
			_outStream.Write(f, 0, f.Length);
			//var pageContent = File.OpenRead(scriptPath);
			ThreadPool.QueueUserWorkItem((x) =>
											{
												Thread.Sleep(5000);
												scriptPath = Path.Combine(SiteBasePath, "Site.Scripts.jquery.mobile-1.1.1.min.js");
												f = File.ReadAllBytes(scriptPath);
												_outStream.Position = 0;
												_outStream.Write(f, 0, f.Length);
												_outStream.Position = _outStream.Length - f.Length;
											});
			OperationContext clientContext = OperationContext.Current;
			clientContext.OperationCompleted += new EventHandler(delegate(object sender, EventArgs args)
			{

			});

			_outStream.Position = 0;
			return _outStream;
			return _outStream;
		}

		[OperationBehavior]
		public Stream LoadContent(string contentPath)
		{
			var scriptPath = Path.Combine(SiteBasePath, "Site." + contentPath.Replace('/', '.'));

			if (WebOperationContext.Current != null)
			{
				WebOperationContext.Current.OutgoingResponse.ContentType = MimeTypes.GetMimeType(scriptPath);
			//	WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.Expires, "Fri, 30 Jul 1970 06:47:23 GMT");
				WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.Connection, "Keep-Alive");
			//	WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.TransferEncoding, "chunked");
			//	WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.KeepAlive, "true");
			}

			if(File.Exists(scriptPath))
			{
				var pageContent = File.OpenRead(scriptPath);
				return pageContent;
			}

			if (WebOperationContext.Current != null)
			{
				WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotFound;
			}

			return null;

		}


		private static string SiteBasePath
		{
			get
			{
				var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				path = Path.Combine(path, "WebController");
				if(!Directory.Exists(path))
				{
					Directory.CreateDirectory(path);
				}

				return path;
			}
		}

		

		private static string GetSiteFolderPath(string relativeFolderPath)
		{
			return Path.Combine(SiteBasePath, relativeFolderPath); 
		}

		public class DelayableStream : Stream
		{
			private Stream _stream;
			private bool _isDataAvailable = true;
			public DelayableStream(Stream stream)
			{
				_stream = stream;
			}

			public override bool CanRead
			{
				get { return _stream.CanRead; }
			}

			public override bool CanSeek
			{
				get { return _stream.CanSeek; }
			}

			public override bool CanWrite
			{
				get { return _stream.CanWrite; }
			}

			public override void Flush()
			{
				_stream.Flush();
			}

			public override long Length
			{
				get { return _stream.Length + 1; }
			}

			public override long Position
			{
				get { return _stream.Position; }
				set { _stream.Position = value; }
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				int ct = 0;
				if(!_isDataAvailable)
				{
					
				}
				ct = _stream.Read(buffer, offset, count);
				if(_stream.Position == _stream.Length - 1)
				{
					_isDataAvailable = false;
				}

				

				return ct;
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				return _stream.Seek(offset, origin);

			}

			public override void SetLength(long value)
			{
				_stream.SetLength(value);
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				_isDataAvailable = true;
			}
		}
	}
}
