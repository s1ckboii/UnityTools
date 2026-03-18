UnityTools is an ongoing side project for utilities I create while working on my multiplayer fps towerdefense game.

These tools will be stripped to the core, so you'll need to expand on it.

The reason why the editortool here is so useful is mainly because you can just simply create a prefab of a waypoint / ainode whatever you call it,
and then just drag on drop into the scene, it'll align perfectly on the surface and once you are done with all, you can just select them all and place them in a parent object,
which has the `EnemyPath` script on it. You won't need to worry about their Y-axis anymore, you also get a clear view on how they would move at all times, not just when selected.

(To expand on it, you'll need to work on the Editor too. For example I have my `Guardian`s that I need to reference,
```cs
        [SerializeField] private Guardian guardian;
        public Guardian Guardian => guardian;
```

for that to show up, I need to add them in `OnInspectorGUI()`

```cs
            SerializedProperty guardianProp = serializedObject.FindProperty("guardian");
            EditorGUILayout.PropertyField(guardianProp);
```

(Also noting that the `[HideInInspector]` was not necessary as I changed my logic since then and it effectively doesn't do anything..)

The first tool I made is a simple Enemy Path script with cool editor design.
`EnemyPath.cs` is a very basic script to connect waypoints from one and to another which I'm using for my enemies.

Its Editor script though is a bit more interesting. It makes the script automatically register waypoints added as childobjects. Its locked by default,

<img width="316" height="259" alt="image" src="https://github.com/user-attachments/assets/b797f51d-4bbd-4a02-a344-372681196bc2" />
<img width="316" height="465" alt="image" src="https://github.com/user-attachments/assets/25c61536-1425-4f8f-90f3-ae37dc8abade" />



but you can unlock it if you'd like to move the order around. The order in the list changes the order for childobjects too, making the line refresh itself and properly change shape.

<img width="1473" height="638" alt="image" src="https://github.com/user-attachments/assets/4ce9e3ac-654f-4279-bd4e-a8beb6b04977" />
<img width="1465" height="604" alt="image" src="https://github.com/user-attachments/assets/bc2718ee-e61f-4e73-854b-bec00f601296" />


It is also designed in a way so you can see the lines in editor all the time BUT if you'd like to hide them, you can do that with just a button. You can select multiple to hide them all at the same time or unhide them.

Lastly the editor also has a color wheel, if you would like to have a different color theme. The last selected waypoint also shows up in the same color.

<img width="500" height="443" alt="image" src="https://github.com/user-attachments/assets/ecda2515-697b-4ce3-bcdb-8c122cc186f7" />
<img width="676" height="500" alt="image" src="https://github.com/user-attachments/assets/b10830cf-eca8-49f6-bdbc-2d76437282f3" />


I made four assets in Figma (I might remake the eye icons) just to give them even more personality. The buttons scale with the inspector. (Might need to scale the text too, will need to see how it looks)

<img width="260" height="469" alt="image" src="https://github.com/user-attachments/assets/4f374968-d5c8-480c-bebf-92432b498639" />
<img width="848" height="445" alt="image" src="https://github.com/user-attachments/assets/6797aeac-244b-4140-942a-bca7fd73a6d5" />

Now we also have `EnemyPathGraph`, later I'll rename these two but I'm having some weird behaviours whenever I push new updates so uh... I'll have to fix it first.
<img width="1417" height="588" alt="image" src="https://github.com/user-attachments/assets/c6418cad-cca1-4906-9660-49268b045616" />

And I'll make a proper readme and move some info to folders so they dont keep piling up this readme. You can disable the discs or the lines seperately or disable both with the graph visible button. Connection is distance based you give it yourself, and if they overlap they connect, if they overlap in a way where the intersection has both midpoints you'll have "perfect" connection (means its green). Though I would not go for that as ai nodes do confuse enemies and also can be performance heavy if done poorly. This is a very basic tool, responsibility lies on you how you use it.
