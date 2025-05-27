using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VL.Rive
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var x = CreateRenderContextD3D11(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);


            // This is a placeholder for the main entry point of the application.
            // You can add your application logic here.
            Console.WriteLine("Welcome to VL.Rive!");
            Console.ReadLine(); // Wait for user input before closing

        }

        [DllImport("rive_interop.dll")]
        static extern IntPtr CreateRenderContextD3D11(IntPtr a, IntPtr b, IntPtr c);
    }
}
