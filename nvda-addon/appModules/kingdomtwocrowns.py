# Kingdom Access - NVDA app module for Kingdom Two Crowns (KingdomTwoCrowns.exe).
#
# NVDA stops speaking whenever a key is pressed. In a game every key press is a game action,
# so the mod's announcements would be cut all the time. Sleep mode makes NVDA leave the
# keyboard to the game and not interrupt speech; the Kingdom Access mod keeps speaking through
# the NVDA controller client. NVDA+Shift+S still toggles sleep mode manually.

import appModuleHandler


class AppModule(appModuleHandler.AppModule):
	sleepMode = True
