using System;
using System.Collections.Generic;
using Switchboard.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>Unscaled 2D victory timeline. All generated objects belong to one disposable modal canvas.</summary>
    public sealed class VictorySequenceView : MonoBehaviour
    {
        private const float PlaybackSpeed = 1.5f;
        public const float Duration = 26f / PlaybackSpeed;
        [SerializeField] private Sprite frameSprite, buttonSprite;
        [SerializeField] private TMP_FontAsset titleFont, bodyFont;
        [SerializeField] private Sprite[] portraits;
        [SerializeField] private AudioClip connectSound, clickSound;
        private GameObject overlay;
        private RectTransform panel, town, skyline, farSkyline;
        private CanvasGroup panelGroup, titleGroup, controls;
        private VictoryTownGraphic roads;
        private readonly List<RectTransform> buildings = new List<RectTransform>();
        private readonly List<RectTransform> badges = new List<RectTransform>();
        private readonly List<CableGraphic> wires = new List<CableGraphic>();
        private readonly List<RectTransform> tips = new List<RectTransform>();
        private readonly List<LampGlowGraphic> glows = new List<LampGlowGraphic>();
        private AudioSource audioSource;
        private Action continueGame, mainMenu;
        private float elapsed;
        private int soundedConnections;
        public bool IsOpen => overlay != null && overlay.activeSelf;
        public bool IsFinished => elapsed >= Duration;
        public Canvas PresentationCanvas => overlay != null ? overlay.GetComponent<Canvas>() : null;
        private static readonly Color Cream = new Color(1,.96f,.81f);
        // Dense blocks rather than detached houses. Back-to-front ordering keeps the street frontage readable.
        private static readonly Vector2[] Sites = CreateSites();
        private static readonly int[] Residents = { 0, 2, 4, 6, 8, 10, 12, 14, 15, 17, 19, 21, 23 };
        // Disjoint pairs: every resident participates in at most one connection.
        // The thirteenth portrait is still present, without reusing anyone to make an extra pair.
        private static readonly int[,] Links = { {0,8}, {5,2}, {10,3}, {1,11}, {9,6}, {7,4} };

        private static Vector2[] CreateSites()
        {
            var result = new Vector2[24];
            for (int row = 0; row < 3; row++)
                for (int column = 0; column < 8; column++)
                    result[row * 8 + column] = new Vector2(-717.5f + column * 205, 100 - row * 180 + (column % 3) * 5);
            return result;
        }
        private static float BuildingHeight(int site) => 145 + (site * 7 % 5) * 19;
        // Two concurrent connections per wave, interleaved across town with unequal travel times.
        private static float WireStart(int wire) => 6.8f + (wire / 2) * 4.2f + (wire * 7 % 5) * .18f;
        private static float WireDuration(int wire) => 2.8f + (wire * 5 % 7) * .17f;

        public void Play(Action onContinue, Action onMainMenu)
        {
            if (IsOpen) return;
            continueGame = onContinue; mainMenu = onMainMenu;
            if (overlay == null) Build();
            overlay.SetActive(true);
            elapsed = 0; soundedConnections = 0;
            RenderAt(0);
        }

        public void Hide()
        {
            if (overlay != null) overlay.SetActive(false);
            continueGame = mainMenu = null;
        }

        private void Update()
        {
            if (!IsOpen || IsFinished) return;
            var keyboard = Keyboard.current;
            if (elapsed < Duration && keyboard != null &&
                (keyboard.escapeKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) Skip();
            RenderAt(Mathf.Min(Duration, elapsed + Time.unscaledDeltaTime));
            int connections = 0;
            for (int i = 0; i < Links.GetLength(0); i++)
                if (elapsed * PlaybackSpeed >= WireStart(i) + WireDuration(i)) connections++;
            if (connections > soundedConnections)
            {
                if (connectSound != null && audioSource != null) audioSource.PlayOneShot(connectSound, .45f);
                soundedConnections = connections;
            }
        }

        public void Skip()
        {
            soundedConnections = Links.GetLength(0);
            RenderAt(Duration);
        }

        // Also used by the isolated editor render/check command; no gameplay or save state is changed here.
        public void RenderAt(float seconds)
        {
            if (overlay == null) return;
            elapsed = Mathf.Clamp(seconds,0,Duration);
            float timeline = elapsed * PlaybackSpeed;
            float enter = Smooth(timeline/.65f);
            panelGroup.alpha = enter;
            panel.localScale = Vector3.one;
            float camera = Smooth((timeline-.8f)/24.4f);
            town.localScale = Vector3.one * Mathf.Lerp(1.04f,.94f,camera);
            town.anchoredPosition = new Vector2(0,Mathf.Lerp(-180,-195,camera));
            farSkyline.anchoredPosition = new Vector2(0,Mathf.Lerp(0,-90,camera));
            skyline.anchoredPosition = new Vector2(0,Mathf.Lerp(-95,-180,camera));
            roads.Reveal = Mathf.Clamp01((timeline-.7f)/2.5f); roads.SetVerticesDirty();
            for(int i=0;i<buildings.Count;i++) buildings[i].localScale = Vector3.one * Pop((timeline-1.1f-(i*11%24)*.075f)/.7f);
            for(int i=0;i<badges.Count;i++)
            {
                badges[i].localScale = Vector3.one * Pop((timeline-3.65f-(i*7%Residents.Length)*.13f)/.6f);
                glows[i].color = new Color(1,.83f,.43f,timeline>6.8f ? .55f+.1f*Mathf.Sin(timeline*1.7f+i) : .18f);
            }
            for(int i=0;i<wires.Count;i++)
            {
                float t=Smooth((timeline-WireStart(i))/WireDuration(i));
                wires[i].gameObject.SetActive(t>0);
                Vector2 a=badges[Links[i,0]].anchoredPosition, b=badges[Links[i,1]].anchoredPosition;
                a.y-=46; b.y-=46;
                Vector2 end=Vector2.Lerp(a,b,t);
                wires[i].SetEndpoints(a,end);
                tips[i].anchoredPosition=end; tips[i].gameObject.SetActive(t>0);
            }
            titleGroup.alpha=Smooth((timeline-21)/2f);
            controls.alpha=Smooth((timeline-23.5f)/1.5f);
            controls.interactable=controls.blocksRaycasts=timeline>=25f;
        }

        private static float Smooth(float t) => Mathf.SmoothStep(0,1,Mathf.Clamp01(t));
        private static float Pop(float t)
        {
            if(t<=0)return 0;if(t>=1)return 1;
            float u=t-1;return 1+2.2f*u*u*u+1.2f*u*u;
        }

        private void Build()
        {
            var root=Rect("Victory presentation",transform,Vector2.zero,new Vector2(1920,1080));
            overlay=root.gameObject;
            var canvas=overlay.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.overrideSorting=true;canvas.sortingOrder=100;
            var scaler=overlay.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            overlay.AddComponent<GraphicRaycaster>();
            audioSource=overlay.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=0;
            overlay.AddComponent<AudioChannelSource>().Configure(AudioVolumeChannel.Sfx,.4f);
            var blocker=Image("Backdrop",root,Vector2.zero,Vector2.zero,null,new Color(.05f,.06f,.05f,1));
            Stretch(blocker.rectTransform);
            blocker.raycastTarget=true;
            panel=Rect("Panel",root,Vector2.zero,Vector2.zero);
            Stretch(panel);
            panelGroup=panel.gameObject.AddComponent<CanvasGroup>();
            var viewport=Rect("City viewport",panel,Vector2.zero,Vector2.zero);
            Stretch(viewport);
            var sky=Art("Sky",viewport,Vector2.zero,Vector2.zero,VictoryTownGraphic.Shape.Sky,Color.white);
            Stretch(sky.rectTransform);
            var sun=Rect("Sunlight",viewport,new Vector2(570,160),new Vector2(230,230));
            sun.gameObject.AddComponent<LampGlowGraphic>().color=new Color(1,.81f,.43f,.45f);
            Circle("Sun",viewport,new Vector2(570,160),100,new Color(1,.87f,.61f,.9f));
            farSkyline=Art("Distant skyline",viewport,Vector2.zero,new Vector2(1800,380),VictoryTownGraphic.Shape.Skyline,new Color(.47f,.49f,.45f)).rectTransform;
            skyline=Art("Near skyline",viewport,new Vector2(0,-95),new Vector2(1800,380),VictoryTownGraphic.Shape.Skyline,new Color(.39f,.42f,.36f)).rectTransform;
            skyline.GetComponent<VictoryTownGraphic>().Variant=2;
            town=Rect("Town camera",viewport,Vector2.zero,new Vector2(1700,850));
            roads=Art("Streets",town,Vector2.zero,town.sizeDelta,VictoryTownGraphic.Shape.Roads,Cream);
            for(int i=0;i<Sites.Length;i++)
            {
                var house=Art("House "+i,town,Sites[i],new Vector2(164+i%3*6,BuildingHeight(i)),VictoryTownGraphic.Shape.Building,Color.white);
                house.Variant=i;house.rectTransform.pivot=new Vector2(.5f,0);buildings.Add(house.rectTransform);
            }
            var wireRoot=Rect("Connections",town,Vector2.zero,town.sizeDelta);
            var badgeRoot=Rect("Residents",town,Vector2.zero,town.sizeDelta);
            for(int i=0;i<Residents.Length;i++)
            {
                int site=Residents[i];Vector2 pos=Sites[site]+Vector2.up*(BuildingHeight(site)+46);
                var badge=Rect("Resident "+i,badgeRoot,pos,new Vector2(94,94));badges.Add(badge);
                var glow=Rect("Connection glow",badge,Vector2.zero,new Vector2(155,155)).gameObject.AddComponent<LampGlowGraphic>();
                glow.raycastTarget=false;glows.Add(glow);
                Circle("Gold rim",badge,Vector2.zero,100,new Color(.89f,.73f,.35f));
                Circle("Dark mount",badge,Vector2.zero,92,new Color(.2f,.25f,.21f));
                Image("Portrait",badge,Vector2.zero,new Vector2(84,84),portraits!=null&&portraits.Length>0?portraits[i%portraits.Length]:null,Color.white).preserveAspect=true;
                Circle("Brass jack",badge,new Vector2(0,-46),13,new Color(.93f,.75f,.36f));
            }
            Color[] colors={new Color(.72f,.23f,.16f),new Color(.78f,.66f,.23f),new Color(.25f,.49f,.60f)};
            for(int i=0;i<Links.GetLength(0);i++)
            {
                var wire=Rect("Wire "+i,wireRoot,Vector2.zero,town.sizeDelta).gameObject.AddComponent<CableGraphic>();
                wire.raycastTarget=false;wire.InsulationColor=colors[i%3];wire.ThicknessScale=.65f;wire.MaximumSag=55+(i*17%65);wires.Add(wire);
                tips.Add(Circle("Plug "+i,wireRoot,Vector2.zero,12,new Color(.94f,.77f,.4f)).rectTransform);
            }
            var title=Label("Thank you",panel,new Vector2(0,366),new Vector2(900,175),"Thank you for\nconnecting us!",72,titleFont);
            title.color=new Color(1,.9f,.61f);titleGroup=title.gameObject.AddComponent<CanvasGroup>();
            var shadow=title.gameObject.AddComponent<Shadow>();shadow.effectDistance=new Vector2(2,-3);shadow.effectColor=new Color(.18f,.16f,.11f,.65f);
            var actions=Rect("Final actions",panel,new Vector2(0,-434),new Vector2(750,72));controls=actions.gameObject.AddComponent<CanvasGroup>();
            MakeButton("Main Menu",actions,new Vector2(-190,0),new Vector2(340,72),36,()=>Finish(false));
            MakeButton("Keep Playing",actions,new Vector2(190,0),new Vector2(340,72),36,()=>Finish(true));
        }

        private void Finish(bool keep)
        {
            Action action=keep?continueGame:mainMenu;
            // Play on the existing persistent UI sound source so closing the canvas cannot cut the click off.
            if(keep && clickSound!=null)
            {
                foreach(var ui in FindObjectsByType<SceneUiAudio>(FindObjectsSortMode.None))
                { var source=ui.GetComponent<AudioSource>();if(source!=null)source.PlayOneShot(clickSound);break; }
            }
            Hide();action?.Invoke();
        }

        private Button MakeButton(string text,Transform parent,Vector2 position,Vector2 size,float fontSize,Action action)
        {
            var image=Image(text,parent,position,size,buttonSprite,Color.white);image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            button.navigation=new Navigation{mode=Navigation.Mode.None};button.transition=Selectable.Transition.ColorTint;
            var colors=button.colors;colors.normalColor=new Color(.92f,.92f,.92f);colors.highlightedColor=colors.selectedColor=Color.white;
            colors.pressedColor=new Color(.72f,.72f,.72f);colors.fadeDuration=.1f;button.colors=colors;
            Label("Label",image.transform,new Vector2(0,3.3f),size,text,fontSize,bodyFont);
            button.onClick.AddListener(()=>action());return button;
        }

        private static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
        {
            var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;
            r.gameObject.layer=5;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);
            r.anchoredPosition=pos;r.sizeDelta=size;return r;
        }
        private static Image Image(string name,Transform parent,Vector2 pos,Vector2 size,Sprite sprite,Color color)
        {
            var image=Rect(name,parent,pos,size).gameObject.AddComponent<Image>();image.sprite=sprite;image.color=color;image.raycastTarget=false;return image;
        }
        private static CircleGraphic Circle(string name,Transform parent,Vector2 pos,float size,Color color)
        {
            var circle=Rect(name,parent,pos,Vector2.one*size).gameObject.AddComponent<CircleGraphic>();circle.color=color;circle.raycastTarget=false;return circle;
        }
        private static VictoryTownGraphic Art(string name,Transform parent,Vector2 pos,Vector2 size,VictoryTownGraphic.Shape kind,Color color)
        {
            var art=Rect(name,parent,pos,size).gameObject.AddComponent<VictoryTownGraphic>();art.Artwork=kind;art.color=color;art.raycastTarget=false;return art;
        }
        private static TMP_Text Label(string name,Transform parent,Vector2 pos,Vector2 size,string text,float fontSize,TMP_FontAsset font)
        {
            var label=Rect(name,parent,pos,size).gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.text=text;
            label.fontSize=fontSize;label.color=Cream;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
            label.textWrappingMode=TextWrappingModes.NoWrap;return label;
        }
        private void OnDisable() => Hide();
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
