from sanic import Sanic
from sanic.log import logger
from jinja2 import FileSystemLoader, Environment
from motor import motor_asyncio as motor
from discordwebhook import Discord
import os
import glob
import importlib
import dotenv

dotenv.load_dotenv()
app = Sanic("api.fasmga")
app.ctx.webhook = Discord(url = os.getenv("DiscordWebHook"))
app.ctx.jinja = Environment(loader = FileSystemLoader(searchpath = "./html"))
app.debug = os.getenv("developer") == "true"

#region plug-in handling

if app.debug: logger.warning("You are running into developer mode, make sure you don't are using this for run production!")
logger.info(f"Discovering plug-in for {app.name}!")
logger.info("-------------------------------------------------------")
for filename in [os.path.basename(f)[:-3] for f in glob.glob(os.path.join(os.path.dirname("./sources/plugin/"), "*.py")) if os.path.isfile(f)]:
	module = importlib.import_module(f"sources.plugin.{filename}")
	logger.info(f"Loading {filename}...")
	try:
		module.plug_in()
	except Exception as error:
		logger.error(f"Error loading {filename}")
		if app.debug: logger.error(error)
		else: logger.info("To see error set 'developer' env value to 'true'")
	else: 
		logger.info(f"{filename} loaded successfully!")
	logger.info("-------------------------------------------------------")
logger.info("Done!")

#endregion
#region Motor Setup

@app.listener("before_server_start")
def setupMotor(app, loop):
	logger.info("Opening motor connection to database")
	app.ctx.database = motor.AsyncIOMotorClient(os.getenv("MongoDB"), io_loop=loop)
	app.ctx.db = app.ctx.database.fasmga

@app.listener("before_server_stop")
def closingMotor(app, loop):
	logger.info("Closing motor conntenction")
	app.ctx.database.close()

#endregion

if app.debug: 
	app.run(debug = True, auto_reload = True)
else: 
	app.run()