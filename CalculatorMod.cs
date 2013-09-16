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
		MethodInfo updateBattleChat;
		MethodInfo ChatMessage;
		public CalculatorMod() {
			//private void ChatMessage(string roomName, string from, string text, bool isWhisper)
			Type s = typeof(string);
			ChatMessage = typeof(ChatRooms).GetMethod("ChatMessage", BindingFlags.Instance | BindingFlags.NonPublic);
			updateBattleChat  = typeof(BattleMode).GetMethod("updateChat", BindingFlags.NonPublic | BindingFlags.Instance);
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
				scrollsTypes["Communicator"].Methods.GetMethod("sendSilentRequest", new Type[]{typeof(Message)}),
				scrollsTypes["BattleMode"].Methods.GetMethod("handleMessage", new Type[]{typeof(Message)}),
			};
		}
		public static string GetName() {
			return "Calculator";
		}
		public static int GetVersion() {
			return 1;
		}

		private BattleMode bm = null;
		public override void BeforeInvoke (InvocationInfo info) {
			if (info.targetMethod.Equals ("handleMessage")) {
				if (info.arguments [0] is GameInfoMessage) {
					bm = (BattleMode)info.target;
				}
			}
		}

		public override void AfterInvoke (InvocationInfo info, ref object returnValue) {
		}

		public override bool WantsToReplace(InvocationInfo info) {
			if (info.targetMethod.Equals("sendSilentRequest")) {
				if (info.arguments [0] is RoomChatMessageMessage) {
					RoomChatMessageMessage msg = (RoomChatMessageMessage)info.arguments [0];
					return msg.text.StartsWith("/calc ") || msg.text.StartsWith("/c ");
				} else if (info.arguments [0] is WhisperMessage) {
					WhisperMessage msg = (WhisperMessage)info.arguments [0];
					return msg.text.StartsWith("/calc ") || msg.text.StartsWith("/c ");
				} else if(info.arguments[0] is GameChatMessageMessage) {
					GameChatMessageMessage msg = (GameChatMessageMessage)info.arguments [0];
					return msg.text.StartsWith("/calc ") || msg.text.StartsWith("/c ");
				}
			}
			return false;
		}

		public override void ReplaceMethod (InvocationInfo info, out object returnValue) {
			string text;
			bool inGameChat = false;
			if (info.arguments [0] is RoomChatMessageMessage) {
				RoomChatMessageMessage msg = (RoomChatMessageMessage)info.arguments [0];
				text = msg.text;
			} else if (info.arguments [0] is WhisperMessage) {
				WhisperMessage msg = (WhisperMessage)info.arguments [0];
				text = msg.text;
			} else if (info.arguments[0] is GameChatMessageMessage) {
				GameChatMessageMessage msg = (GameChatMessageMessage)info.arguments [0];
				text = msg.text;
				inGameChat = true;
			} else {
				returnValue = true;
				return;
			}

			text = text.Substring(text.IndexOf (" "));
			double result;
			try {
				result = Evaluate(text);
				SendMessage(text + " = " + result, inGameChat);
			} catch {
				SendMessage(text + "FAILED", inGameChat);
			}

			returnValue = true;
		}


		public void SendMessage(String message, bool inGameChat) {
			if (!inGameChat) {
				var room = App.ArenaChat.ChatRooms.GetCurrentRoom ();
				if (room == null)
					return;

				ChatMessage.Invoke (App.ArenaChat.ChatRooms, new Object[] { room.name, "CalculatorMod", message, true });
				//TODO: Scroll the Chat-Window
			} else {
				updateBattleChat.Invoke(bm, new String[] { "CalculatorMod: " + message });
			}

		}
	}
}

