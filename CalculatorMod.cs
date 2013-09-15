using System;
using System.Text.RegularExpressions;
using System.Xml.XPath;
using System.IO;
using System.Reflection;
using ScrollsModLoader.Interfaces;
using Mono.Cecil;
namespace CalculatorMod
{
	public class CalculatorMod : BaseMod
	{
		MethodInfo ChatMessage;
		public CalculatorMod() {
			//private void ChatMessage(string roomName, string from, string text, bool isWhisper)
			Type s = typeof(string);
			ChatMessage = typeof(ChatRooms).GetMethod("ChatMessage", BindingFlags.Instance | BindingFlags.NonPublic);
		}
		//http://stackoverflow.com/a/2196685/2256700
		public static double Evaluate(string expression)
		{
			var xsltExpression = 
				string.Format("number({0})", 
				              new Regex(@"([\+\-\*])").Replace(expression, " ${1} ")
				              .Replace("/", " div ")
				              .Replace("%", " mod "));

			return (double)new XPathDocument
				(new StringReader("<r/>"))
					.CreateNavigator()
					.Evaluate(xsltExpression);
		}

		public static MethodDefinition[] GetHooks(TypeDefinitionCollection scrollsTypes, int version) {
			return new MethodDefinition[] {
				scrollsTypes["Communicator"].Methods.GetMethod("sendRequest", new Type[]{typeof(Message)}),
				//typeof(ChatUI).GetMethod ("Initiate", BindingFlags.Public | BindingFlags.Instance)
			};
		}
		public static string GetName() {
			return "Calculator";
		}
		public static int GetVersion() {
			return 1;
		}

		public override void BeforeInvoke (InvocationInfo info) {
		}

		public override void AfterInvoke (InvocationInfo info, ref object returnValue) {
		}

		public override bool WantsToReplace(InvocationInfo info) {
			if (info.targetMethod.Equals("sendRequest")) {
				if (info.arguments [0] is RoomChatMessageMessage) {
					RoomChatMessageMessage msg = (RoomChatMessageMessage)info.arguments [0];
					return msg.text.StartsWith("/calc ") || msg.text.StartsWith("/c ");
				} else if (info.arguments [0] is WhisperMessage) {
					WhisperMessage msg = (WhisperMessage)info.arguments [0];
					return msg.text.StartsWith("/calc ") || msg.text.StartsWith("/c ");
				}
			}
			return false;
		}

		public override void ReplaceMethod (InvocationInfo info, out object returnValue) {
			string text;
			if (info.arguments [0] is RoomChatMessageMessage) {
				RoomChatMessageMessage msg = (RoomChatMessageMessage)info.arguments [0];
				text = msg.text;
			} else if (info.arguments [0] is WhisperMessage) {
				WhisperMessage msg = (WhisperMessage)info.arguments [0];
				text = msg.text;
			} else {
				returnValue = null;
				return;
			}

			text = text.Substring(text.IndexOf (" "));
			double result;
			try {
				result = Evaluate(text);
				SendMessage(text + " = " + result);
			} catch {
				SendMessage(text + "FAILED");
			}

			returnValue = null;
		}


		public void SendMessage(String message) {
			var room = App.ArenaChat.ChatRooms.GetCurrentRoom();
			if( room == null )
				return;

			ChatMessage.Invoke (App.ArenaChat.ChatRooms, new Object[] { room.name, "CalculatorMod", message, true });
			//TODO: Scroll Chat-Window
		}
	}
}

