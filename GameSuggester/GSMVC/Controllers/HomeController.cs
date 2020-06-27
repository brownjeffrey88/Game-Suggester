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
using System.Xml.XPath;

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

        public async Task<IActionResult> DisplayGame(Request request)
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
            games = await GetCollection(collectionURL, request);


            //verify user inputs are added to url
            string thingURL = bggBaseUrl + "thing?id=";
            //List<GameModel> gameOptions = await GetGames(thingURL, games);
            GameModel gameOption = await GetGame(thingURL, games);

            //from here we can pass the game list to a function to remove games from the list that dont match our criteria and return the list
            //depending on an option the user chose we will either show the whole list or a random game

            //test stuff delete after complete
            request.username = collectionURL;
            Console.WriteLine(request.username);
            List <Request> names = new List<Request>();
            names.Add(request);

            //should return a game model
            //return DisplayGames(gameOptions);
            return View(gameOption);

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
        public async Task<List<String>> GetCollection(string url, Request parameters)
        {
            List<String> gameId = new List<string>();
            List<XElement> gameNodes = await GetElementList(url);
            for (int i = 0; i < gameNodes.Count; i++)
            {

                XElement game = gameNodes[i];
                //disregard expansions
                if (game.Attribute("subtype").Value == "boardgame")
                {
                    //if the game has the right amount of players or if the player didnt enter a player count then continue
                    if (parameters.players == null || (parameters.players >= Int32.Parse(game.Element("stats").Attribute("minplayers").Value) && parameters.players <= Int32.Parse(game.Element("stats").Attribute("maxplayers").Value)))
                    {
                        //if the game has a lower playtime than desired or if the player didnt enter a play time
                        if (parameters.playTime == null || (Int32.Parse(game.Element("stats").Attribute("maxplaytime").Value) <= parameters.playTime))
                        {
                            //if the game has a higher rating than requested or if the player didnt specify
                                if (parameters.rating == null || (parameters.rating >= float.Parse(game.Element("stats").Element("rating").Element("average").Attribute("value").Value)))
                                {
                                    string gameValue = game.Attribute("objectid").Value;
                                    gameId.Add(gameValue);
                                }
                        }
                    }
                }
                else
                {
                    continue;
                }
            }



            return gameId;
        }

        public async Task<GameModel> GetGame(string url, List<string> gameList)
        {
            //use for individual games, pick a game from the list at random, remove it from the list. check its parameters, if params are good send it otherwise recursive call a new one.
            int games = gameList.Count;
            GameModel game = new GameModel();
            Random rand = new Random();
            int pick = rand.Next(0, games);
            XElement gameElement = await GetElement(url + gameList[pick] + "&stats=1");


            game.Name = gameElement.Element("item").Element("name").Attribute("value").Value;
            game.Image = gameElement.Element("item").Element("image").Value;
            game.Description = gameElement.Element("item").Element("description").Value;
            game.Rating = float.Parse(gameElement.Element("item").Element("statistics").Element("ratings").Element("average").Attribute("value").Value);
            game.Designers = gameElement.Elements()
                .Where(e => e.Name == "item").Elements()
                .Where(e => e.Name == "link")
                .Select(e => e.Attribute("type"))
                .Where(e => e.Value == "boardgamedesigner")
                .Select(e => e.Parent).Attributes()
                .Where(e => e.Name == "value")
                .Select(e => e.Value);
            game.Artists = gameElement.Elements()
                .Where(e => e.Name == "item").Elements()
                .Where(e => e.Name == "link")
                .Select(e => e.Attribute("type"))
                .Where(e => e.Value == "boardgameartist")
                .Select(e => e.Parent).Attributes()
                .Where(e => e.Name == "value")
                .Select(e => e.Value);
            game.Publishers = gameElement.Elements()
                .Where(e => e.Name == "item").Elements()
                .Where(e => e.Name == "link")
                .Select(e => e.Attribute("type"))
                .Where(e => e.Value == "boardgamepublisher")
                .Select(e => e.Parent).Attributes()
                .Where(e => e.Name == "value")
                .Select(e => e.Value);
            game.MinPlayers = Int32.Parse(gameElement.Element("item").Element("minplayers").Attribute("value").Value);
            game.Solo = (game.MinPlayers == 1) ? true : false;
            game.MaxPlayers = Int32.Parse(gameElement.Element("item").Element("maxplayers").Attribute("value").Value);
            game.MinPlayTime = Int32.Parse(gameElement.Element("item").Element("minplaytime").Attribute("value").Value);
            game.MaxPlayTime = Int32.Parse(gameElement.Element("item").Element("maxplaytime").Attribute("value").Value);
            game.BggRank = 1; //Int32.Parse(gameElement.Element("item").Element("statistics").Element("ratings").Element("ranks").Element("rank").Attribute("value").Value);
            game.weight = float.Parse(gameElement.Element("item").Element("statistics").Element("ratings").Element("averageweight").Attribute("value").Value);
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
            game.SuggestedPlayers = 5;
            // found on page, key terms: "polls":{"userplayers":{"best":[{"min":3,"max":3}],"recommended":[{"min":1,"max":4}]

            gameList.Remove(gameList[pick]);

            //run checks on game, if its good continue, if not recursive.

            return game;
        }
        public async Task<List<GameModel>> GetGames(string url, List<String> gameList)
        {


            List<GameModel> choices = new List<GameModel>();
            foreach (var id in gameList)
            {
                //make a new game model, populate it, add it to the game model list
                GameModel game = new GameModel();
                XElement gameElement = await GetElement(url + id + "&stats=1");

                    game.Image = gameElement.Element("item").Element("image").Value;
                    game.Description = gameElement.Element("item").Element("description").Value;
                    game.Rating = float.Parse(gameElement.Element("item").Element("statistics").Element("ratings").Element("average").Attribute("value").Value);
                    game.Designers = gameElement.Elements()
                        .Where(e => e.Name == "item").Elements()
                        .Where(e => e.Name == "link")
                        .Select(e => e.Attribute("type"))
                        .Where(e => e.Value == "boardgamedesigner")
                        .Select(e => e.Parent).Attributes()
                        .Where(e => e.Name == "value")
                        .Select(e => e.Value);
                    game.Artists = gameElement.Elements()
                        .Where(e => e.Name == "item").Elements()
                        .Where(e => e.Name == "link")
                        .Select(e => e.Attribute("type"))
                        .Where(e => e.Value == "boardgameartist")
                        .Select(e => e.Parent).Attributes()
                        .Where(e => e.Name == "value")
                        .Select(e => e.Value);
                    game.Publishers = gameElement.Elements()
                        .Where(e => e.Name == "item").Elements()
                        .Where(e => e.Name == "link")
                        .Select(e => e.Attribute("type"))
                        .Where(e => e.Value == "boardgamepublisher")
                        .Select(e => e.Parent).Attributes()
                        .Where(e => e.Name == "value")
                        .Select(e => e.Value);
                    game.MinPlayers = Int32.Parse(gameElement.Element("item").Element("minplayers").Attribute("value").Value);
                    game.Solo = (game.MinPlayers == 1) ? true : false;
                    game.MaxPlayers = Int32.Parse(gameElement.Element("item").Element("maxplayers").Attribute("value").Value);
                    game.MinPlayTime = Int32.Parse(gameElement.Element("item").Element("minplaytime").Attribute("value").Value);
                    game.MaxPlayTime = Int32.Parse(gameElement.Element("item").Element("maxplaytime").Attribute("value").Value);
                    game.BggRank = 1; //Int32.Parse(gameElement.Element("item").Element("statistics").Element("ratings").Element("ranks").Element("rank").Attribute("value").Value);
                    game.weight = float.Parse(gameElement.Element("item").Element("statistics").Element("ratings").Element("averageweight").Attribute("value").Value);
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
                    game.SuggestedPlayers = 5;
                    // found on page, key terms: "polls":{"userplayers":{"best":[{"min":3,"max":3}],"recommended":[{"min":1,"max":4}]
                    choices.Add(game);
            }
            return choices;
        }


        public IActionResult DisplayGames(List<GameModel> allGames)
        {
            return View(allGames);
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
