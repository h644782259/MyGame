from pathlib import Path
r=Path(__file__).resolve().parents[1]/'Assets/Scripts'
u=(r/'UI/GameUI.cs').read_text(); lifecycle=(r/'UI/GameUI.Lifecycle.cs').read_text(); expedition=(r/'UI/GameUI.Expedition.cs').read_text(); m=(r/'UI/MobileControls.cs').read_text(); s=(r/'Core/GameSession.cs').read_text(); a=(r/'Core/GameAudio.cs').read_text()
assert 'GUI.enabled = !session.BackgroundPaused && !LifecycleTouchBlocked && !UITransitionBlocked' in u
cancel=expedition.split('public void CancelBackgroundInput()',1)[1].split('\n        }',1)[0]
for token in ['GUIUtility.hotControl=0','GUIUtility.keyboardControl=0','lifecycleRelease.Block','CancelHotbarPointer()','BlockUITransition()']: assert token in cancel,token
assert 'Screen.dpi' in lifecycle and 'Screen.orientation' in lifecycle
assert 'MobileControls.ResetInput();' in lifecycle and 'CancelBackgroundInput();' in lifecycle
assert m.index('ui.RefreshTouchViewport()')<m.index('ui.LifecycleTouchBlocked')
assert 'backConsumedFrame!=Time.frameCount' in lifecycle
assert 'panel==Panel.None' in lifecycle and '!exitRequest.Open' in lifecycle
update=u.split('private void Update()',1)[1].split('private void OnDestroy()',1)[0]
assert update.index('bool gameplayBackAllowed=GameplayBackAllowed')<update.index('backConsumedFrame=Time.frameCount')<update.index('if(exitRequest.Open)')
assert 'gameplayBackAllowed && charge != null' in update and 'gameplayBackAllowed && targeting != null' in update
assert update.index('if(exitRequest.Open)')<update.index('if (!session.HasStarted)')
assert 'else if(AndroidBackExitEnabled&&panel==Panel.None)RequestExit(false)' in update
assert 'exitRequest.Confirm' not in update and 'Application.Quit' not in update
exitbody=s.split('public bool ExitApplication(',1)[1].split('\n        }',1)[0]
assert '#if UNITY_IOS' in exitbody and 'UNITY_ANDROID' not in exitbody and 'Application.Quit()' in exitbody
assert s.count('GameAudio.SetBackgroundPaused(pauseState.BackgroundPaused)')==2
assert 'private const int VoiceCount = 8' in a and 'ignoreListenerPause = true' in a
assert 'lifecycle.BackgroundPaused || index' in a and 'lifecycle.BackgroundPaused || !isActiveAndEnabled' in a
assert '!lifecycle.BackgroundPaused && ambientSource' in a
assert 'transition==AudioLifecycleTransition.None||instance==null' in a
assert 'ambientSource.Pause()' in a and 'ambientSource.UnPause()' in a
assert 'ambientResumeSample<0&&ambientSource.clip!=null' in a
assert 'ambientSource.timeSamples=ambientResumeSample%ambientClip.samples' in a
print('PASS: Android lifecycle production wiring (background UI, viewport, Back priority, save/audio transitions)')
