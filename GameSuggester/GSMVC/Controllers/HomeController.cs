using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using GSMVC.Models;
using System.Net.Http;
using System.Xml.Linq;

namespace GSMVC.Controllers
{
    public class HomeController : Controller
    {

        //BGG api docs https://boardgamegeek.com/wiki/page/BGG_XML_API2

        private readonly ILogger<HomeController> _logger;
        string bggBaseUrl = "https://boardgamegeek.com/xmlapi2/";

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

            //fancy animation idea: rotate images of their games on the screen while crunching api with "picking a game" text

                //string url = bggBaseUrl + "collection?username=" + request.username;
                //make sure username is valid. if result returned
                //take all parameters from the game request, apply the fields relevant to the collection-API request to the url and execute
                //all other fields will be added to a search-criteria-type model and held
                //collection-API xml results will be parsed and stored in a list.
                //each id in that list will be searched and xml parsed, anything not matching the search-criteria-type model will be removed from the list
                //once complete a random game will be displayed and then removed from the list. if "pick a different game" is selected the next game will follow the same procedure.

        public async Task<IActionResult> DisplayUserAsync(Request request)
        {


            //TODO
            //ADD "SHOW ME A LIST" OR "PICK AT RANDOM" BUTTONS



            List<String> games = new List<string>();
            //remembers the users name for next visit
            if (request.remember)
            {
                //do cookie stuff
            }

            //takes the request model and gets the username filled in by the user
            string collectionURL = bggBaseUrl + "collection?username=" + request.username + "&stats=1&own=1";

            //sees if the player only wants suggestions for games in their collection they havent played
            if (request.unplayed)
            {
                collectionURL += "&played=0";
            }



            //TODO
            //slider min rating and rating or minbggrating bggrating -- if played = 0 use bgg rating
            //checkboxes -cooperative competitive solo



            //takes collection url, sends to get list of game objectid's to parse using get game
            games = await GetCollection(collectionURL);


            //verify user inputs are added to url
            string thingURL = bggBaseUrl + "thing?id=";
            List<GameModel> gameOptions = await GetGames(thingURL, games);

            //from here we can pass the game list to a function to remove games from the list that dont match our criteria and return the list
            //depending on an option the user chose we will either show the whole list or a random game

            //test stuff delete after complete
            request.username = collectionURL;
            Console.WriteLine(request.username);
            List <Request> names = new List<Request>();
            names.Add(request);

            //should return a game model
            return View(names);
        }

        //passes in a BGG url and returns single element, used for game xml parsing
        public async Task<XElement> GetElement(string url)
        {
            //make element 
            
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync(url))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    XElement responseToXml = XElement.Parse(apiResponse);
                    return responseToXml;
                }
            }

            
        }

        //passes in a BGG url and returns a list of parsed items, will use for collection xml parsing
        public async Task<List<XElement>> GetElementList(string url)
        {
            //make element list 
            List<XElement> gameNodes = new List<XElement>();
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync(url))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    XElement responseToXml = XElement.Parse(apiResponse);
                    gameNodes = responseToXml.Elements("item").ToList();
                }
            }

            return gameNodes;
        }

        //takes collection url, gets the elements of the returned xml, parses them and removes game object id's for use with GetGames function
        public async Task<List<String>> GetCollection(string url)
        {
            List<String> gameId = new List<string>();
            List<XElement> gameNodes = await GetElementList(url);
            for (int i = 0; i < gameNodes.Count; i++)
            {
                XElement game = gameNodes[i];
                string gameValue = game.Attribute("objectid").Value;



                //if min players max players, rank were wrong then continue, else add to the list




                gameId.Add(gameValue);
            }
            return gameId;
        }


        public async Task<List<GameModel>> GetGames(string url, List<String> gameList)
        {
            List<GameModel> choices = new List<GameModel>();
            foreach (var id in gameList)
            {
                //make a new game model, populate it, add it to the game model list
                GameModel game = new GameModel();
                XElement gameElement = await GetElement(url + id + "&stats=1");
                if(gameElement.Attribute("type").Value == "boardgameexpansion")
                {
                    continue;
                }
                else
                {
                    game.Image = gameElement.Attribute("image").Value;
                    game.Description = gameElement.Attribute("description").Value;
                    game.Rating = float.Parse(gameElement.Element("statistics").Element("ratings").Attribute("average").Value);
                    game.Designer = gameElement.Attribute("boardgamedesigner").Value;
                    game.Artist = gameElement.Attribute("boardgameartist").Value;
                    game.Publisher = gameElement.Attribute("boardgamepublisher").Value;
                    game.MinPlayers = Int32.Parse(gameElement.Attribute("minplayers").Value);
                    game.Solo = (game.MinPlayers == 1) ? true : false;
                    game.MaxPlayers = Int32.Parse(gameElement.Attribute("maxplayers").Value);
                    game.MinPlayTime = Int32.Parse(gameElement.Attribute("minplaytime").Value);
                    game.MaxPlayTime = Int32.Parse(gameElement.Attribute("maxplaytime").Value);
                    game.BggRank = Int32.Parse(gameElement.Element("statistics").Element("ranks").Attribute("value").Value);
                    game.weight = float.Parse(gameElement.Element("statistics").Element("ratings").Attribute("averageweight").Value);
                    game.Mechanics = gameElement.Elements()
                        .Where(e => e.Name == "item").Elements()
                        .Where(e => e.Name == "link")
                        .Select(e => e.Attribute("type"))
                        .Where(e => e.Value == "boardgamemechanic")
                        .Select(e => e.Parent).Attributes()
                        .Where(e => e.Name == "value")
                        .Select(e => e.Value);

                    //use a web scraper to get the below information, easier than trying to parse the elements
                    //https://boardgamegeek.com/boardgame/ followed by games ID
                    //try this web scraper https://dev.to/rachelsoderberg/create-a-simple-web-scraper-in-c-1l1m

                    //suggested players-string need to parse page or loop through object elements to find highest suggested.

                    //need to sort
                    game.SuggestedPlayers = Int32.Parse(gameElement.Attribute("").Value);
                    // found on page, key terms: "polls":{"userplayers":{"best":[{"min":3,"max":3}],"recommended":[{"min":1,"max":4}]

                }


            }

            return choices;
        }





        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
