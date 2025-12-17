# Advanced Visual Effects for Event UI

## Overview

Beyond the modern card layout, you can add **stunning visual effects** to make your events even more immersive and engaging. This guide covers advanced techniques for animations, transitions, particles, and cinematic effects.

## 1. Animated Transitions

### Fade-In Animation

Add smooth fade-in when the screen appears:

```xml
<BrushWidget Id="EventCard" WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" 
             SuggestedWidth="1100" SuggestedHeight="750" 
             AlphaFactor="0">
    <Children>
        <!-- Card contents -->
    </Children>
    <!-- Fade in over 0.3 seconds -->
    <AnimationState Name="FadeIn">
        <AnimationTrack Parameter="AlphaFactor" StartKey="0" EndKey="1" Duration="0.3" />
    </AnimationState>
</BrushWidget>
```

### Slide-In Animation

Make the card slide up from the bottom:

```xml
<BrushWidget Id="EventCard" PositionYOffset="200">
    <AnimationState Name="SlideUp">
        <AnimationTrack Parameter="PositionYOffset" StartKey="200" EndKey="0" Duration="0.4" Easing="QuadOut" />
        <AnimationTrack Parameter="AlphaFactor" StartKey="0" EndKey="1" Duration="0.4" />
    </AnimationState>
</BrushWidget>
```

### Staggered Choice Buttons

Animate choices appearing one by one:

```xml
<ButtonWidget DelayedAlphaFactor="0" DelayTime="@DelayTime">
    <AnimationState Name="StaggerIn">
        <AnimationTrack Parameter="AlphaFactor" StartKey="0" EndKey="1" Duration="0.2" />
        <AnimationTrack Parameter="PositionXOffset" StartKey="-50" EndKey="0" Duration="0.2" />
    </AnimationState>
</ButtonWidget>
```

In ViewModel:
```csharp
// Set staggered delay for each choice
choice.DelayTime = index * 0.1f; // 100ms between each
```

## 2. Hover Effects

### Glowing Buttons

Add glow effect on hover:

```xml
<ButtonWidget UpdateChildrenStates="true">
    <Children>
        <BrushWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent">
            <VisualState State="Default">
                <Animation Name="Default">
                    <AnimationTrack Parameter="Color" Value="#FFFFFF" />
                </Animation>
            </VisualState>
            <VisualState State="Hovered">
                <Animation Name="Hover">
                    <AnimationTrack Parameter="Color" Value="#FFFFAA" Duration="0.15" />
                    <AnimationTrack Parameter="Brightness" StartKey="1.0" EndKey="1.3" Duration="0.15" />
                </Animation>
            </VisualState>
        </BrushWidget>
    </Children>
</ButtonWidget>
```

### Scale on Hover

Make buttons slightly larger when hovered:

```xml
<ButtonWidget ScaleFactor="1.0">
    <VisualState State="Hovered">
        <Animation Name="ScaleUp">
            <AnimationTrack Parameter="ScaleFactor" StartKey="1.0" EndKey="1.05" Duration="0.1" />
        </Animation>
    </VisualState>
</ButtonWidget>
```

### Shadow Effect

Add drop shadow that intensifies on hover:

```xml
<Widget DropShadowIntensity="0.3">
    <VisualState State="Hovered">
        <Animation Name="ShadowGrow">
            <AnimationTrack Parameter="DropShadowIntensity" StartKey="0.3" EndKey="0.7" Duration="0.15" />
        </Animation>
    </VisualState>
</Widget>
```

## 3. Particle Effects

### Fire Particles (for heated moments)

Add fire particles when Heat is high:

```csharp
public class LanceLifeEventVM : ViewModel
{
    private ParticleEmitter _fireParticles;
    
    private void UpdateEscalationTracks()
    {
        var heat = escalation.GetHeat();
        
        // Show fire particles when heat > 7
        if (heat > 7)
        {
            ShowFireParticles();
        }
        else
        {
            HideFireParticles();
        }
    }
    
    private void ShowFireParticles()
    {
        if (_fireParticles == null)
        {
            // Create particle emitter at top of heat bar
            _fireParticles = new ParticleEmitter("fire_small");
            _fireParticles.Position = GetHeatBarPosition();
            _fireParticles.Rate = 10; // 10 particles per second
        }
    }
}
```

### Sparkle Effect (for skill rewards)

Add sparkles when choices grant XP:

```xml
<!-- In LanceLifeEventScreen.xml (choice template is inlined) -->
<ParticleEmitterWidget Id="RewardSparkles" 
                       ParticleEffect="sparkle_gold"
                       IsVisible="@HasSkillRewards"
                       EmissionRate="5"
                       LifeSpan="1.0"
                       PositionXOffset="@RewardTextXOffset"
                       PositionYOffset="@RewardTextYOffset">
    <ParticleSettings>
        <StartColor R="1.0" G="0.9" B="0.3" A="1.0" />
        <EndColor R="1.0" G="0.9" B="0.3" A="0.0" />
        <StartSize="3" EndSize="0" />
        <Velocity X="0" Y="-20" Z="0" />
        <Spread X="10" Y="5" Z="0" />
    </ParticleSettings>
</ParticleEmitterWidget>
```

### Warning Pulses (for risky choices)

Pulsing red glow for dangerous options:

```xml
<Widget Id="RiskWarning" IsVisible="@IsHighRisk">
    <BrushWidget Sprite="BlankWhiteSquare_9" Color="#FF000044">
        <AnimationState Name="Pulse" Loop="true">
            <AnimationTrack Parameter="Color.A" 
                          StartKey="0.2" 
                          EndKey="0.5" 
                          Duration="0.8" 
                          PingPong="true" />
        </AnimationState>
    </BrushWidget>
</Widget>
```

## 4. Dynamic Backgrounds

### Time-of-Day Tinting

Change background color based on time:

```csharp
private string GetBackgroundTint()
{
    var hour = CampaignTime.Now.CurrentHourInDay;
    
    if (hour < 6)       return "#2233BB44"; // Night - blue
    else if (hour < 12) return "#FFE5AA22"; // Morning - golden
    else if (hour < 18) return "#FFFFFF22"; // Day - neutral
    else if (hour < 21) return "#FF990022"; // Dusk - orange
    else                return "#2233BB44"; // Night - blue
}
```

Apply in XML:
```xml
<BrushWidget Color="@BackgroundTint">
    <!-- Scene image here -->
</BrushWidget>
```

### Parallax Backgrounds

Create depth with multiple background layers:

```xml
<Widget>
    <!-- Far background (slow movement) -->
    <ImageWidget Sprite="background_far" PositionXOffset="@ParallaxFar" />
    
    <!-- Mid background (medium movement) -->
    <ImageWidget Sprite="background_mid" PositionXOffset="@ParallaxMid" />
    
    <!-- Near background (fast movement) -->
    <ImageWidget Sprite="background_near" PositionXOffset="@ParallaxNear" />
</Widget>
```

In ViewModel:
```csharp
protected override void OnFrameTick(float dt)
{
    // Update parallax based on mouse position
    var mouseX = Input.GetMousePositionX();
    var centerX = ScreenWidth / 2;
    var offset = (mouseX - centerX) / ScreenWidth;
    
    ParallaxFar = offset * 10;
    ParallaxMid = offset * 20;
    ParallaxNear = offset * 40;
}
```

### Weather Effects

Add rain, snow, or fog overlays:

```xml
<!-- Rain overlay -->
<Widget IsVisible="@IsRaining">
    <ParticleEmitterWidget ParticleEffect="rain_medium" 
                          EmissionRate="100"
                          Gravity="200">
        <ParticleSettings>
            <StartColor R="0.8" G="0.8" B="1.0" A="0.5" />
            <Velocity X="0" Y="300" Z="0" />
            <LifeSpan="2.0" />
        </ParticleSettings>
    </ParticleEmitterWidget>
</Widget>

<!-- Fog overlay -->
<Widget IsVisible="@IsFoggy">
    <BrushWidget Sprite="fog_overlay" 
                Color="#FFFFFF88"
                AlphaFactor="@FogDensity">
        <AnimationState Name="FogDrift" Loop="true">
            <AnimationTrack Parameter="PositionXOffset" 
                          StartKey="0" 
                          EndKey="100" 
                          Duration="20" />
        </AnimationState>
    </BrushWidget>
</Widget>
```

## 5. Cinematic Effects

### Letterbox Bars

Add cinematic black bars for dramatic moments:

```xml
<!-- Top bar -->
<Widget WidthSizePolicy="StretchToParent" 
       HeightSizePolicy="Fixed" 
       SuggestedHeight="0"
       VerticalAlignment="Top"
       Sprite="BlankWhiteSquare_9"
       Color="#000000FF"
       IsVisible="@IsCinematic">
    <AnimationState Name="LetterboxIn">
        <AnimationTrack Parameter="SuggestedHeight" 
                      StartKey="0" 
                      EndKey="80" 
                      Duration="0.5" />
    </AnimationState>
</Widget>

<!-- Bottom bar (mirror) -->
<Widget WidthSizePolicy="StretchToParent" 
       HeightSizePolicy="Fixed" 
       SuggestedHeight="0"
       VerticalAlignment="Bottom"
       Sprite="BlankWhiteSquare_9"
       Color="#000000FF"
       IsVisible="@IsCinematic">
    <AnimationState Name="LetterboxIn">
        <AnimationTrack Parameter="SuggestedHeight" 
                      StartKey="0" 
                      EndKey="80" 
                      Duration="0.5" />
    </AnimationState>
</Widget>
```

### Camera Shake

Shake the screen for impactful moments:

```csharp
public class LanceLifeEventVM : ViewModel
{
    private float _shakeIntensity;
    private float _shakeTime;
    
    public void TriggerCameraShake(float intensity, float duration)
    {
        _shakeIntensity = intensity;
        _shakeTime = duration;
    }
    
    protected override void OnFrameTick(float dt)
    {
        if (_shakeTime > 0)
        {
            _shakeTime -= dt;
            
            // Calculate shake offset
            var shakeX = MBRandom.RandomFloatRanged(-_shakeIntensity, _shakeIntensity);
            var shakeY = MBRandom.RandomFloatRanged(-_shakeIntensity, _shakeIntensity);
            
            ScreenOffsetX = shakeX;
            ScreenOffsetY = shakeY;
            
            // Decay shake intensity
            _shakeIntensity *= 0.95f;
        }
        else
        {
            ScreenOffsetX = 0;
            ScreenOffsetY = 0;
        }
    }
}
```

Trigger on dramatic choices:
```csharp
private void OnChoiceSelected(EventChoiceVM choice)
{
    if (choice.Option.Effects?.Escalation > 3)
    {
        // Big escalation = camera shake
        TriggerCameraShake(intensity: 5, duration: 0.3f);
    }
}
```

### Slow Motion

Slow down text reveal for emphasis:

```xml
<RichTextWidget Text="@StoryText" 
               TextRevealSpeed="@RevealSpeed"
               TextRevealMode="CharacterByCharacter">
    <!-- Slow reveal for dramatic moments -->
</RichTextWidget>
```

```csharp
private float GetTextRevealSpeed()
{
    if (_event.Category == "escalation")
        return 0.05f; // Slow reveal (50ms per character)
    else
        return 0.01f; // Fast reveal (10ms per character)
}
```

## 6. Sound Effects

### Choice Hover Sound

Play subtle sound on button hover:

```csharp
public class EventChoiceVM : ViewModel
{
    private bool _wasHovered;
    
    protected override void OnRefresh()
    {
        bool isHovered = IsHovered; // From mouse position
        
        if (isHovered && !_wasHovered)
        {
            // Play hover sound
            SoundEvent.PlaySound2D("ui/tick");
        }
        
        _wasHovered = isHovered;
    }
}
```

### Choice Selection Sound

Different sounds for different choice types:

```csharp
private void OnChoiceSelected(EventChoiceVM choice)
{
    // Play sound based on risk
    if (choice.IsRisky)
        SoundEvent.PlaySound2D("ui/warning");
    else if (choice.Option.Costs?.Gold > 0)
        SoundEvent.PlaySound2D("ui/coin");
    else
        SoundEvent.PlaySound2D("ui/click");
    
    ApplyChoiceEffects(choice.Option);
}
```

### Ambient Sound Layers

Add atmospheric audio based on location:

```csharp
private void PlayAmbientSound()
{
    var settlement = _enlistment?.CurrentLord?.PartyBelongedTo?.CurrentSettlement;
    
    if (settlement != null)
    {
        // In settlement
        SoundEvent.CreateEvent("event:/map/ambient/node/settlements/2d/town", Scene.Current);
    }
    else
    {
        // In camp
        SoundEvent.CreateEvent("event:/map/ambient/node/settlements/2d/camp_army", Scene.Current);
    }
}
```

### Music Stingers

Play short musical cues for outcomes:

```csharp
private void PlayOutcomeStinger(LanceLifeEventOptionDefinition option)
{
    if (option.Effects?.LanceReputation > 3)
    {
        // Positive outcome
        SoundEvent.PlaySound2D("event:/music/stinger/positive");
    }
    else if (option.Effects?.Heat > 2)
    {
        // Negative outcome
        SoundEvent.PlaySound2D("event:/music/stinger/negative");
    }
    else
    {
        // Neutral
        SoundEvent.PlaySound2D("event:/music/stinger/neutral");
    }
}
```

## 7. Advanced Layout Effects

### Split Screen Comparison

Show before/after for major choices:

```xml
<Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent">
    <!-- Left side: Current state -->
    <Widget WidthSizePolicy="Fixed" SuggestedWidth="550" HorizontalAlignment="Left">
        <RichTextWidget Text="@CurrentStateText" />
        <CharacterTableauWidget DataSource="{CurrentCharacter}" />
    </Widget>
    
    <!-- Divider -->
    <Widget WidthSizePolicy="Fixed" SuggestedWidth="2" 
           HorizontalAlignment="Center"
           Sprite="BlankWhiteSquare_9"
           Color="#FFFFFF44" />
    
    <!-- Right side: Potential outcome -->
    <Widget WidthSizePolicy="Fixed" SuggestedWidth="550" HorizontalAlignment="Right">
        <RichTextWidget Text="@OutcomePreviewText" />
        <CharacterTableauWidget DataSource="{PotentialCharacter}" />
    </Widget>
</Widget>
```

### Spotlight Effect

Highlight important choices with spotlight:

```xml
<Widget Id="Spotlight" 
       WidthSizePolicy="Fixed" 
       HeightSizePolicy="Fixed"
       SuggestedWidth="200"
       SuggestedHeight="200"
       PositionXOffset="@SpotlightX"
       PositionYOffset="@SpotlightY"
       Sprite="spotlight_gradient"
       Color="#FFFFAA88"
       BlendMode="Additive">
    <AnimationState Name="Pulse" Loop="true">
        <AnimationTrack Parameter="ScaleFactor" 
                      StartKey="1.0" 
                      EndKey="1.2" 
                      Duration="1.5" 
                      PingPong="true" />
    </AnimationState>
</Widget>
```

### Tooltip Cards

Rich tooltips with images and stats:

```xml
<Widget Id="ChoiceTooltip" 
       IsVisible="@IsTooltipVisible"
       PositionXOffset="@TooltipX"
       PositionYOffset="@TooltipY">
    <BrushWidget Brush="Encyclopedia.Frame">
        <Children>
            <!-- Tooltip title -->
            <RichTextWidget Text="@TooltipTitle" Brush.FontSize="16" />
            
            <!-- Divider -->
            <Widget Sprite="divider_line" />
            
            <!-- Success chance -->
            <Widget>
                <RichTextWidget Text="Success Chance:" />
                <Widget Sprite="progressbar_frame">
                    <Widget Sprite="progressbar_fill" 
                           SuggestedWidth="@SuccessBarWidth"
                           Color="#44FF44" />
                </Widget>
                <RichTextWidget Text="@SuccessPercentText" />
            </Widget>
            
            <!-- Rewards breakdown -->
            <RichTextWidget Text="@RewardsBreakdown" />
            
            <!-- Risks breakdown -->
            <RichTextWidget Text="@RisksBreakdown" Color="#FF4444" />
        </Children>
    </BrushWidget>
</Widget>
```

## 8. Performance Optimization

### Lazy Loading

Only load heavy assets when needed:

```csharp
private CharacterTableauWidget _characterTableau;

private void ShowCharacter()
{
    if (_characterTableau == null)
    {
        // Only create tableau when first needed
        _characterTableau = new CharacterTableauWidget();
        _characterTableau.Initialize(_character);
    }
    
    _characterTableau.IsVisible = true;
}
```

### Object Pooling

Reuse particle emitters:

```csharp
public class ParticlePool
{
    private static Dictionary<string, Queue<ParticleEmitter>> _pools = new();
    
    public static ParticleEmitter Get(string effectName)
    {
        if (!_pools.ContainsKey(effectName))
            _pools[effectName] = new Queue<ParticleEmitter>();
        
        if (_pools[effectName].Count > 0)
            return _pools[effectName].Dequeue();
        
        return new ParticleEmitter(effectName);
    }
    
    public static void Return(string effectName, ParticleEmitter emitter)
    {
        emitter.Stop();
        _pools[effectName].Enqueue(emitter);
    }
}
```

### Level of Detail

Reduce effects based on settings:

```csharp
public enum VisualQuality
{
    Low,    // No particles, no animations
    Medium, // Basic animations, limited particles
    High    // Full effects
}

private void UpdateEffects()
{
    var quality = GetVisualQuality();
    
    switch (quality)
    {
        case VisualQuality.Low:
            EnableParticles = false;
            EnableAnimations = false;
            break;
        case VisualQuality.Medium:
            EnableParticles = true;
            ParticleRate = 0.5f;
            EnableAnimations = true;
            break;
        case VisualQuality.High:
            EnableParticles = true;
            ParticleRate = 1.0f;
            EnableAnimations = true;
            break;
    }
}
```

## Example: Complete Cinematic Event

Putting it all together for a dramatic escalation event:

```csharp
public class CinematicEscalationEvent : LanceLifeEventScreen
{
    protected override void OnInitialize()
    {
        base.OnInitialize();
        
        // 1. Fade in letterbox bars
        EnableCinematicMode();
        
        // 2. Play dramatic music
        PlayMusic("event:/music/dramatic/tension");
        
        // 3. Show character with dramatic lighting
        SetCharacterLighting(intensity: 1.5f, color: "#FF4444");
        
        // 4. Slow text reveal
        SetTextRevealSpeed(0.05f);
        
        // 5. Camera shake on reveal
        DelayedAction(2.0f, () => TriggerCameraShake(8, 0.5f));
        
        // 6. Fire particles on heat bar
        ShowFireParticles();
        
        // 7. Pulsing warning on risky choice
        EnableWarningPulse(choiceIndex: 0);
    }
    
    protected override void OnChoiceSelected(EventChoiceVM choice)
    {
        // Play dramatic sound
        PlayOutcomeStinger(choice.Option);
        
        // Camera shake for big choices
        if (choice.Option.Effects?.Heat > 3)
        {
            TriggerCameraShake(10, 0.6f);
        }
        
        // Slow motion outcome reveal
        Time.TimeScale = 0.5f;
        DelayedAction(1.0f, () => Time.TimeScale = 1.0f);
        
        base.OnChoiceSelected(choice);
    }
}
```

## Conclusion

With these advanced techniques, you can create **truly cinematic event experiences** that rival AAA RPGs. The key is to:

1. **Layer effects** - Combine animations, particles, and sound
2. **Match intensity** - Scale effects to event importance
3. **Optimize performance** - Use lazy loading and object pooling
4. **Test thoroughly** - Ensure effects enhance rather than distract

Start with basic animations, then gradually add more advanced effects as you become comfortable with the system!
