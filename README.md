# ShepardFilter

A Unity package for applying the classic Shepard/Risset effect as a filter to an AudioSource.

## Usage

Simply attach `ShepardFilter.cs` to a GameObject with an AudioSource component.

You can modify the many parameters using the Inspector or via scripting.

The effect will not move by itself, you must do this yourself by calling the `ShepardFilter.Shift()` function.

For example, for an endlessly moving filter corresponding to the player's vertical position:

```C#
void Update(){
    GetComponent<ShepardFilter>.Shift(player.transform.y % 1);
}
```

## Description of Parameters

### Mix
A value between 0-1 determining the mixture between dry sound and wet Shepard filtered sound
Set using `ShepardFilter.SetMix(float)`

### Q
A single Q value shared between all of the filters.
Set using `ShepardFilter.SetQ(float)`

### Center Frequency
The frequency at which filters are the loudest.
Set using `ShepardFilter.SetCenter(float)`

### Width
The span of filter cutoff frequencies from the center frequency, in *linear octaves*
Set using `ShepardFilter.SetWidth(float)`

### Rolloff
The rate at which filters decrease as they move away from the center frequency.  Values between 0-1 are recommended, though values above 1 are possible.
Set using `ShepardFilter.SetRolloff(float)`
