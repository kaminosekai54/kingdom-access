# Kingdom Access - NVDA app module for Kingdom Two Crowns (KingdomTwoCrowns.exe).
#
# NVDA normally stops speaking whenever a key is pressed. In a game every key press is a game
# action, so the Kingdom Access announcements would be cut all the time. While the game has the
# focus, this module stops key presses from interrupting speech; the mod itself interrupts
# speech when it has something new to say. Normal NVDA behaviour comes back as soon as another
# window gets the focus.
#
# Sleep mode is NOT used: in sleep mode NVDA also ignores the speech sent by applications,
# which would silence the mod.

import appModuleHandler
from keyboardHandler import KeyboardInputGesture

_originalEffect = KeyboardInputGesture.speechEffectWhenExecuted


def _noInterrupt(self):
	return None


class AppModule(appModuleHandler.AppModule):

	def event_appModule_gainFocus(self):
		KeyboardInputGesture.speechEffectWhenExecuted = property(_noInterrupt)

	def event_appModule_loseFocus(self):
		KeyboardInputGesture.speechEffectWhenExecuted = _originalEffect

	def terminate(self):
		KeyboardInputGesture.speechEffectWhenExecuted = _originalEffect
		super().terminate()
