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
        public const float Duration = 14f;
        [SerializeField] private Sprite frameSprite, buttonSprite;
        [SerializeField] private TMP_FontAsset titleFont, bodyFont;
        [SerializeField] private Sprite[] portraits;
        [SerializeField] private AudioClip connectSound, clickSound;
        private GameObject overlay;
        private RectTransform panel, town, hills, farHills;
        private CanvasGroup panelGroup, titleGroup, controls;
        private VictoryTownGraphic roads;
        private readonly List<RectTransform> buildings = new List<RectTransform>();
        private readonly List<RectTransform> badges = new List<RectTransform>();
        private readonly List<CableGraphic> wires = new List<CableGraphic>();
        private readonly List<RectTransform> tips = new List<RectTransform>();
        private readonly List<LampGlowGraphic> glows = new List<LampGlowGraphic>();
        private Button skip;
        private AudioSource audioSource;
        private Action continueGame, mainMenu;
        private float elapsed;
        private int soundedConnections;
        public bool IsOpen => overlay != null && overlay.activeSelf;
        public bool IsFinished => elapsed >= Duration;
        public Canvas PresentationCanvas => overlay != null ? overlay.GetComponent<Canvas>() : null;
        private static readonly Color Cream = new Color(1,.96f,.81f);
        private static readonly Vector2[] Sites = {
            new Vector2(-620,175), new Vector2(-370,180), new Vector2(-115,190), new Vector2(170,205), new Vector2(470,185), new Vector2(690,240),
            new Vector2(-660,-85), new Vector2(-445,-65), new Vector2(-135,-95), new Vector2(100,-30), new Vector2(350,-70), new Vector2(650,-15),
            new Vector2(-550,-285), new Vector2(-230,-290), new Vector2(70,-245), new Vector2(460,-270) };
        private static readonly int[] Residents = { 1, 3, 5, 6, 9, 11 };
        private static readonly int[,] Links = { {0,1}, {1,2}, {0,3}, {3,4}, {4,5}, {1,4}, {2,5} };

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
            int connections = Mathf.Clamp(Mathf.FloorToInt((elapsed - 5.5f) / .43f) + 1, 0, Links.GetLength(0));
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
            float enter = Smooth(elapsed/.65f);
            panelGroup.alpha = enter;
            panel.localScale = Vector3.one * Mathf.Lerp(.93f,1,enter);
            float camera = Smooth((elapsed-.8f)/10.5f);
            town.localScale = Vector3.one * Mathf.Lerp(1.14f,.68f,camera);
            town.anchoredPosition = new Vector2(0,Mathf.Lerp(-30,-265,camera));
            farHills.anchoredPosition = new Vector2(0,Mathf.Lerp(0,-110,camera));
            hills.anchoredPosition = new Vector2(0,Mathf.Lerp(-95,-195,camera));
            roads.Reveal = Mathf.Clamp01((elapsed-.7f)/2.5f); roads.SetVerticesDirty();
            for(int i=0;i<buildings.Count;i++) buildings[i].localScale = Vector3.one * Pop((elapsed-1.25f-i*.12f)/.65f);
            for(int i=0;i<badges.Count;i++)
            {
                badges[i].localScale = Vector3.one * Pop((elapsed-3.35f-i*.23f)/.6f);
                glows[i].color = new Color(1,.83f,.43f,elapsed>5.1f ? .55f+.1f*Mathf.Sin(elapsed*1.7f+i) : .18f);
            }
            for(int i=0;i<wires.Count;i++)
            {
                float t=Smooth((elapsed-5-i*.43f)/.55f);
                wires[i].gameObject.SetActive(t>0);
                Vector2 a=badges[Links[i,0]].anchoredPosition, b=badges[Links[i,1]].anchoredPosition;
                a.y-=46; b.y-=46;
                Vector2 end=Vector2.Lerp(a,b,t);
                wires[i].SetEndpoints(a,end);
                tips[i].anchoredPosition=end; tips[i].gameObject.SetActive(t>0);
            }
            titleGroup.alpha=Smooth((elapsed-10.5f)/1.5f);
            controls.alpha=Smooth((elapsed-12)/1.2f);
            controls.interactable=controls.blocksRaycasts=elapsed>=13.2f;
            skip.gameObject.SetActive(elapsed<13.2f);
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
            var blocker=Image("Backdrop",root,Vector2.zero,new Vector2(4000,3000),null,new Color(.05f,.06f,.05f,.88f));
            blocker.raycastTarget=true;
            panel=Rect("Panel",root,Vector2.zero,new Vector2(1770,970));panelGroup=panel.gameObject.AddComponent<CanvasGroup>();
            Image("Frame",panel,Vector2.zero,panel.sizeDelta,frameSprite,new Color(1,1,1));
            var viewport=Rect("City viewport",panel,new Vector2(0,-5),new Vector2(1580,820));
            viewport.gameObject.AddComponent<RectMask2D>();
            Art("Sky",viewport,Vector2.zero,viewport.sizeDelta,VictoryTownGraphic.Shape.Sky,Color.white);
            var sun=Rect("Sunlight",viewport,new Vector2(570,255),new Vector2(230,230));
            sun.gameObject.AddComponent<LampGlowGraphic>().color=new Color(1,.81f,.43f,.45f);
            Circle("Sun",viewport,new Vector2(570,255),100,new Color(1,.87f,.61f,.9f));
            farHills=Art("Distant hills",viewport,Vector2.zero,new Vector2(1800,500),VictoryTownGraphic.Shape.Hills,new Color(.43f,.54f,.47f)).rectTransform;
            hills=Art("Near hills",viewport,new Vector2(0,-95),new Vector2(1800,500),VictoryTownGraphic.Shape.Hills,new Color(.35f,.47f,.34f)).rectTransform;
            hills.GetComponent<VictoryTownGraphic>().Variant=2;
            town=Rect("Town camera",viewport,Vector2.zero,new Vector2(1600,700));
            roads=Art("Roads",town,Vector2.zero,town.sizeDelta,VictoryTownGraphic.Shape.Roads,Cream);
            for(int i=0;i<28;i++)
            {
                var tree=Art("Tree "+i,town,new Vector2(-730+(i*173)%1460,-290+(i*113)%570),new Vector2(44,78),
                    VictoryTownGraphic.Shape.Tree,i%2==0?new Color(.23f,.37f,.27f):new Color(.42f,.48f,.28f));
                tree.rectTransform.pivot=new Vector2(.5f,0);
            }
            for(int i=0;i<Sites.Length;i++)
            {
                var house=Art("House "+i,town,Sites[i],new Vector2(105+i%3*14,112+i%4*17),VictoryTownGraphic.Shape.House,Color.white);
                house.Variant=i;house.rectTransform.pivot=new Vector2(.5f,0);buildings.Add(house.rectTransform);
            }
            var wireRoot=Rect("Connections",town,Vector2.zero,town.sizeDelta);
            var badgeRoot=Rect("Residents",town,Vector2.zero,town.sizeDelta);
            for(int i=0;i<Residents.Length;i++)
            {
                int site=Residents[i];Vector2 pos=Sites[site]+Vector2.up*(112+site%4*17+46);
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
                wire.raycastTarget=false;wire.InsulationColor=colors[i%3];wire.ThicknessScale=.75f;wire.MaximumSag=85;wires.Add(wire);
                tips.Add(Circle("Plug "+i,wireRoot,Vector2.zero,12,new Color(.94f,.77f,.4f)).rectTransform);
            }
            var title=Label("Thank you",panel,new Vector2(0,265),new Vector2(1340,225),"thank you for\nconnecting us!",82,titleFont);
            title.color=new Color(1,.9f,.61f);titleGroup=title.gameObject.AddComponent<CanvasGroup>();
            var shadow=title.gameObject.AddComponent<Shadow>();shadow.effectDistance=new Vector2(2,-3);shadow.effectColor=new Color(.18f,.16f,.11f,.65f);
            var actions=Rect("Final actions",panel,new Vector2(0,105),new Vector2(750,72));controls=actions.gameObject.AddComponent<CanvasGroup>();
            MakeButton("Main Menu",actions,new Vector2(-190,0),new Vector2(340,72),36,()=>Finish(false));
            MakeButton("Keep Playing",actions,new Vector2(190,0),new Vector2(340,72),36,()=>Finish(true));
            skip=MakeButton("Skip",panel,new Vector2(720,400),new Vector2(150,44),24,Skip);
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
    }
}
