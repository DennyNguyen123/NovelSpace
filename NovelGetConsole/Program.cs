using System;
using System.ComponentModel.Design;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DataSharedLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using NovelGetConsole;

namespace GetTruyen
{
    public class Program
    {

        public static async Task Main(string[] args)
        {

            var getTruyen = new GetTruyen();
            var isLoad = await getTruyen.InitBrowser();

            if (isLoad ?? false == true)
            {
                await getTruyen.GetContentByList();
            }

            //ProcessArguments(args);

            Console.WriteLine("Done");


            Console.ReadKey();

        }
    }
}