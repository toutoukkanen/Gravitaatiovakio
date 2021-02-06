# Gravitaatiovakio

Gravitaatiovakio is a 2D space shooter game made with the Unity game engine (2020.2.2f2).
I originally planned it to be a spiritual successor to Space Invaders but at this stage
I must admit that there are pretty much none similarities anymore other than shooting and space.

Gravitaatiovakio was done on my second year of studying computer science. It was
done to learn more about different algorithms, game development and the whole workflow.
It took me one month to reach the state as it is now when writing this.
It is a short game and it might be rough around the edges but there are many cool
mechanics that gave me a real challenge and a great time when developing them.

If you have anything to say or suggest, please contact me. I'm always eager to learn more.

## Mechanical overview

In the game we are in zero gravity. The enemy has to be able to follow the player
effectively and be able to dodge space debris (or the player itself). This is done
by precisely monitoring the acceleration of the enemy ship and changing it according to the laws of the physics.
Check the code:
https://github.com/toutoukkanen/Gravitaatiovakio/blob/master/Assets/Scripts/Enemy/Strategy/Movement/AdvancedMovementStrategy.cs

No projectile travels at the speed of the light. The enemy has to think carefully
if it wants to hit at anything at all. Luck is on their side for the enemies have a strategic advisor at their disposal
which can simulate the battlefield. For example, the advisor takes to account the weapon's projectile speed,
enemy's own speed and the player's speed. With this information, the advisor tries to predict where the player might go and 
at which angle the hit is most probable.
Check the code:
https://github.com/toutoukkanen/Gravitaatiovakio/blob/master/Assets/Scripts/Enemy/Strategy/Shooting/AdvancedShootingStrategy.cs

Everything in the game is based on blocks. Every spaceship is only a collection of
neon colored squares or triangles.

Integrity calculations for sections. Every spaceship, debris or any other collection
with multiple parts is called a Section. A section manages a lot of things but arguably
the most important one is integrity. Integrity checks give the section the ability
to break apart effectively creating new sections. This makes it possible to break a
spaceship in half or split a flying meteor to multiple parts.
Check the code:
https://github.com/toutoukkanen/Gravitaatiovakio/blob/master/Assets/Scripts/Section.cs

Simple strength calculations for individual blocks. Every block resists attacks
the best only from certain directions based on the block's properties which are defined as integrity vectors.
Check the code:
https://github.com/toutoukkanen/Gravitaatiovakio/blob/master/Assets/Scripts/Part.cs
