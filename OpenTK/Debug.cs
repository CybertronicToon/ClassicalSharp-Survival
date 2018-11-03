using System;

namespace OpenTK {
	
	/// <summary> Placeholder for System.Diagnostics.Debug class because it crashes on some Mono version on Linux. </summary>
	public static class Debug {
		
		public static void Print(string text) {
			try { Console.WriteLine(text); } catch { } 
			// raised by Mono sometimes when trying to write to console from the finalizer thread.
		}
		
        public static void WriteLine(string message) {
			try { Console.WriteLine(message); } catch { } 
		}
		
        public static void WriteLine(object obj) {
			try { Console.WriteLine(obj); } catch { } 
		}
		
		public static void Print(string text, params object[] args) {
			try { Console.WriteLine(text, args); } catch { }
		}
        
        public static void Indent() { }
        public static void Unindent() { }
	}
}
