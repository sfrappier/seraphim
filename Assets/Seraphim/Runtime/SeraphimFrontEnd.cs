using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Seraphim
{
    public sealed class SeraphimFrontEnd : MonoBehaviour
    {
        public enum ScreenState { Playing,MainMenu,MenuSettings,GameSettings }
        SanctuaryGame game;
        Canvas canvas;
        GameObject shade,main,settings,gear;
        Button start,settingsButton,back,returnToMenu;
        Slider music,effects;
        Text musicValue,effectsValue,credit,backLabel;
        EventSystem events;
        StandaloneInputModule legacyUi;
        InputSystemUIInputModule menuUi;
        Font titleFont,bodyFont;
        bool controlsReady=true;
        float armAt;
        public ScreenState State { get; private set; }
        public bool InputEnabled { get; set; }=true;
        public bool IsMainMenu => State==ScreenState.MainMenu || State==ScreenState.MenuSettings;
        public bool IsSettings => State==ScreenState.MenuSettings || State==ScreenState.GameSettings;
        public bool BlocksWorldPointer => State!=ScreenState.Playing || !controlsReady;
        public Button StartButton => start;
        public Slider MusicSlider => music;
        public Slider EffectsSlider => effects;
        public Canvas MenuCanvas => canvas;

        public void Initialize(SanctuaryGame owner)
        {
            game=owner; titleFont=game.models.hudDisplayFont; bodyFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            canvas=new GameObject("Seraphim front end",typeof(RectTransform)).AddComponent<Canvas>(); canvas.transform.SetParent(transform,false);
            canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=Camera.main; canvas.planeDistance=.7f; canvas.sortingOrder=300;
            var scaler=canvas.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1600,900); scaler.matchWidthOrHeight=0;
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            events=FindFirstObjectByType<EventSystem>(); legacyUi=events.GetComponent<StandaloneInputModule>();
            menuUi=events.gameObject.AddComponent<InputSystemUIInputModule>(); menuUi.enabled=false;
            if (menuUi.actionsAsset==null) menuUi.AssignDefaultActions();
            shade=Full(canvas.transform,"Battle shade"); shade.AddComponent<Image>().color=new Color(.005f,.015f,.035f,.48f);
            main=Full(canvas.transform,"Main menu");
            var side=Node(main.transform,"Menu vignette",new Vector2(0,1),new Vector2(0,0),new Vector2(470,900));
            side.pivot=new Vector2(0,1); side.gameObject.AddComponent<Image>().color=new Color(.012f,.025f,.045f,.6f);
            Label(main.transform,"SERAPHIM",new Vector2(58,-60),new Vector2(430,74),46,new Color(.96f,.83f,.53f),true);
            Label(main.transform,"CATHEDRAL OF DAWN",new Vector2(62,-116),new Vector2(340,28),13,new Color(.7f,.79f,.87f));
            start=Button(main.transform,"START",new Vector2(60,-174),new Vector2(282,64),StartGame);
            settingsButton=Button(main.transform,"SETTINGS",new Vector2(60,-258),new Vector2(282,54),OpenSettings);
            Button(main.transform,"QUIT",new Vector2(60,-330),new Vector2(282,54),Quit);
            Label(main.transform,"A six-winged angel. An endless light.",new Vector2(62,-432),new Vector2(355,35),15,new Color(.75f,.8f,.84f));
            Label(main.transform,"v"+Application.version,new Vector2(62,-850),new Vector2(350,25),12,new Color(.52f,.62f,.7f));
            settings=Full(canvas.transform,"Audio settings");
            var panel=Node(settings.transform,"Settings panel",new Vector2(0,1),new Vector2(40,-40),new Vector2(550,540)); panel.pivot=new Vector2(0,1);
            panel.gameObject.AddComponent<Image>().color=new Color(.02f,.035f,.06f,.97f);
            Label(panel,"SETTINGS",new Vector2(30,-22),new Vector2(450,60),32,new Color(.96f,.83f,.53f),true);
            music=VolumeSlider(panel,"MUSIC",120,out musicValue); effects=VolumeSlider(panel,"SOUND EFFECTS",236,out effectsValue);
            credit=Label(panel,"",new Vector2(32,-330),new Vector2(480,32),12,new Color(.63f,.73f,.82f));
            back=Button(panel,"BACK",new Vector2(32,-390),new Vector2(220,52),CloseSettings);
            backLabel=back.GetComponentInChildren<Text>();
            returnToMenu=Button(panel,"MAIN MENU",new Vector2(276,-390),new Vector2(240,52),ShowMainMenu);
            Label(panel,"Escape / Circle / B to return",new Vector2(32,-467),new Vector2(480,28),12,new Color(.63f,.73f,.82f));
            music.onValueChanged.AddListener(value=> { game.Audio.SetMusicVolume(value); RefreshAudioLabels(); });
            effects.onValueChanged.AddListener(value=> { game.Audio.SetEffectsVolume(value); game.Audio.Play(SoundCue.Ui); RefreshAudioLabels(); });
            gear=Button(canvas.transform,"SETTINGS",new Vector2(342,-24),new Vector2(100,28),OpenSettings,10).gameObject;
            State=ScreenState.Playing; SetOverlay(false); RefreshAudioLabels();
        }
        GameObject Full(Transform parent,string name)
        {
            var rect=Node(parent,name,Vector2.zero,Vector2.zero,Vector2.zero); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; return rect.gameObject;
        }
        RectTransform Node(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=anchor; rect.sizeDelta=size; rect.anchoredPosition=position; return rect;
        }
        Text Label(Transform parent,string text,Vector2 position,Vector2 size,int fontSize,Color color,bool heading=false)
        {
            var rect=Node(parent,text,new Vector2(0,1),position,size); rect.pivot=new Vector2(0,1);
            var label=rect.gameObject.AddComponent<Text>(); label.font=heading && titleFont!=null?titleFont:bodyFont;
            label.text=text; label.fontSize=fontSize; label.color=color; label.raycastTarget=false;
            label.alignment=TextAnchor.MiddleLeft; label.horizontalOverflow=HorizontalWrapMode.Wrap; label.verticalOverflow=VerticalWrapMode.Overflow;
            return label;
        }
        Button Button(Transform parent,string text,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action,int fontSize=23)
        {
            var rect=Node(parent,text,new Vector2(0,1),position,size); rect.pivot=new Vector2(0,1);
            var image=rect.gameObject.AddComponent<Image>(); image.color=Color.white;
            var outline=rect.gameObject.AddComponent<Outline>(); outline.effectColor=new Color(.72f,.55f,.25f,.9f); outline.effectDistance=new Vector2(1,-1);
            var button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=image;
            var colors=button.colors; colors.normalColor=new Color(.08f,.12f,.19f); colors.highlightedColor=new Color(.3f,.26f,.16f);
            colors.selectedColor=new Color(.42f,.32f,.14f); colors.pressedColor=new Color(.6f,.45f,.18f); colors.fadeDuration=.08f; button.colors=colors;
            var label=Label(rect,text,new Vector2(0,0),size,fontSize,new Color(.97f,.92f,.78f),fontSize>=20); label.alignment=TextAnchor.MiddleCenter;
            button.onClick.AddListener(()=> { game.Audio.Play(SoundCue.Ui); action(); }); return button;
        }
        Slider VolumeSlider(Transform parent,string text,float y,out Text value)
        {
            Label(parent,text,new Vector2(32,-y),new Vector2(400,30),18,new Color(.94f,.9f,.79f));
            value=Label(parent,"",new Vector2(446,-y),new Vector2(70,30),17,new Color(.96f,.83f,.53f));
            var rect=Node(parent,text+" slider",new Vector2(0,1),new Vector2(32,-y-48),new Vector2(484,30)); rect.pivot=new Vector2(0,1);
            var slider=rect.gameObject.AddComponent<Slider>(); slider.minValue=0; slider.maxValue=1; slider.direction=Slider.Direction.LeftToRight;
            var track=Node(rect,"Track",new Vector2(.5f,.5f),Vector2.zero,new Vector2(484,8)); track.gameObject.AddComponent<Image>().color=new Color(.12f,.18f,.25f);
            var fillArea=Node(rect,"Fill area",new Vector2(.5f,.5f),Vector2.zero,new Vector2(484,8));
            var fill=Node(fillArea,"Volume",Vector2.zero,Vector2.zero,Vector2.zero); fill.anchorMax=Vector2.one; fill.offsetMin=fill.offsetMax=Vector2.zero;
            fill.gameObject.AddComponent<Image>().color=new Color(.79f,.63f,.31f); slider.fillRect=fill;
            var handleArea=Node(rect,"Handle area",new Vector2(.5f,.5f),Vector2.zero,new Vector2(484,30));
            var handle=Node(handleArea,"Volume handle",new Vector2(0,.5f),Vector2.zero,new Vector2(18,30));
            var handleImage=handle.gameObject.AddComponent<Image>(); handleImage.color=new Color(.96f,.87f,.64f);
            slider.handleRect=handle; slider.targetGraphic=handleImage; return slider;
        }
        void RefreshAudioLabels()
        {
            music.SetValueWithoutNotify(game.Audio.MusicVolume); effects.SetValueWithoutNotify(game.Audio.EffectsVolume);
            musicValue.text=Mathf.RoundToInt(game.Audio.MusicVolume*100)+"%"; effectsValue.text=Mathf.RoundToInt(game.Audio.EffectsVolume*100)+"%";
            credit.text=game.Audio.MusicCredit;
        }
        void SetOverlay(bool show)
        {
            shade.SetActive(show); main.SetActive(State==ScreenState.MainMenu); settings.SetActive(IsSettings); gear.SetActive(!show);
            legacyUi.enabled=!show; menuUi.enabled=show; events.sendNavigationEvents=show;
            if (!show) events.SetSelectedGameObject(null);
        }
        void EnableGameplay(bool enabled)
        {
            game.Player.InputEnabled=enabled; game.Skills.InputEnabled=enabled; game.Gamepads.InputEnabled=enabled;
        }
        public void ShowMainMenu()
        {
            Time.timeScale=1; State=ScreenState.MainMenu; controlsReady=false; EnableGameplay(false); game.Hud.SetVisible(false);
            game.Attract.Begin(); SetOverlay(true); RefreshAudioLabels(); Select(start.gameObject);
        }
        public void StartGame()
        {
            if (!IsMainMenu) return;
            game.Attract.End(); Time.timeScale=1; State=ScreenState.Playing; game.Hud.SetVisible(true);
            game.Waves.BeginScenario(); SetOverlay(false); ArmControls();
            game.Audio.Play(SoundCue.Ui);
        }
        public void OpenSettings()
        {
            if (IsSettings) return;
            State=IsMainMenu?ScreenState.MenuSettings:ScreenState.GameSettings;
            if (State==ScreenState.GameSettings) { Time.timeScale=0; game.Audio.StopEffects(); }
            EnableGameplay(false); controlsReady=false; backLabel.text=IsMainMenu?"BACK":"RESUME";
            returnToMenu.gameObject.SetActive(!IsMainMenu); SetOverlay(true); RefreshAudioLabels(); Select(music.gameObject);
        }
        public void CloseSettings()
        {
            if (!IsSettings) return;
            game.Audio.FlushPreferences();
            if (State==ScreenState.MenuSettings) { State=ScreenState.MainMenu; SetOverlay(true); Select(settingsButton.gameObject); }
            else { Time.timeScale=1; State=ScreenState.Playing; SetOverlay(false); ArmControls(); }
        }
        void ArmControls() { controlsReady=false; EnableGameplay(false); armAt=Time.unscaledTime+.15f; }
        void Select(GameObject selected) { StartCoroutine(SelectAfterLayout(selected)); }
        IEnumerator SelectAfterLayout(GameObject selected) { yield return null; if (selected!=null && selected.activeInHierarchy) events.SetSelectedGameObject(selected); }
        bool SubmitHeld()
        {
            if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Return)) return true;
            foreach (var pad in Gamepad.all) if (pad.enabled && (pad.buttonSouth.isPressed || pad.startButton.isPressed)) return true;
            return false;
        }
        void Update()
        {
            if (State==ScreenState.Playing && !controlsReady && Time.unscaledTime>=armAt && !SubmitHeld()) { controlsReady=true; EnableGameplay(true); }
            if (!InputEnabled) return;
            var pause=Input.GetKeyDown(KeyCode.Escape); var cancel=false; var startPressed=false;
            foreach (var pad in Gamepad.all) if (pad.enabled) { startPressed|=pad.startButton.wasPressedThisFrame; cancel|=pad.buttonEast.wasPressedThisFrame; }
            if (IsSettings)
            {
                RefreshAudioLabels();
                if (pause || cancel || startPressed) CloseSettings();
            }
            else if (State==ScreenState.MainMenu) { if (startPressed) StartGame(); else if (pause) OpenSettings(); }
            else
            {
                if ((pause || startPressed) && game.Waves.State!=WaveDirector.EncounterState.Victory) OpenSettings();
            }
        }
        void Quit() { game.Audio.FlushPreferences(); Application.Quit(); }
    }
}
