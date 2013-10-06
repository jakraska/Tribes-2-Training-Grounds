//--------------------------------------//
// TGGame.cs                       //
//--------------------------------------//
// DisplayName = Training Grounds
//--- GAME RULES BEGIN ---
//Welcome to the Training Grounds
//This Gametype is intended for practice purposes
//Respect the rules of the server and have fun
//--- GAME RULES END ---

$InvBanList[TG, "TurretOutdoorDeployable"] = 1;
$InvBanList[TG, "TurretIndoorDeployable"] = 1;
$InvBanList[TG, "ElfBarrelPack"] = 1;
$InvBanList[TG, "MortarBarrelPack"] = 1;
$InvBanList[TG, "PlasmaBarrelPack"] = 1;
$InvBanList[TG, "AABarrelPack"] = 1;
$InvBanList[TG, "MissileBarrelPack"] = 1;
$InvBanList[TG, "Mine"] = 1;
$InvBanList[TG, "PulseSensorDeployable"] = 1;
$InvBanList[TG, "MotionSensorDeployable"] = 1;
$InvBanList[TG, "SatchelCharge"] = 1;
$InvBanList[TG, "FlashGrenade"] = 1;

//Load Race records
exec("prefs/RaceRecords.cs");

if(!$rebuilding)
   {
   exec("scripts/TGStations.cs");
   exec("scripts/TGTrigger.cs");
   exec("scripts/TGSnipingRange.cs");
   exec("scripts/TGOptSetter.cs");
   exec("scripts/TGRace.cs");
   exec("scripts/TGDeathMatch.cs");
   exec("scripts/TGDuel.cs");
   }
package TGGame {
function defaultForceFieldBare::onCollision(%data,%obj,%colobj)
{
 error("WOOT, Collieded with something");
 parent::onCollision(%data,%obj,%colObj);


}
function PhysicalZone::oncollision(%data,%obj,%colobj)
{
   error("MY PZONE ON COLLISION");
   parent::oncollision(%data,%obj,%colobj);
}
function VehicleData::onEnterLiquid(%data, %obj, %coverage, %type)
{
  error("ENTERING MY LIQUID..... <<<<<<<<>>>>>>");
 Parent::onEnterLiquid(%data, %obj, %coverage, %type);
}
function scoutVehicle::onCollision(%this, %obj, %colobj)
{
  error("calling my ON COLLISION for vehicle");
  parent::OnCollision(%this,%obj, %colobj);
}
function Armor::doDismount(%this, %obj, %forced)
{
   if(%obj.InDuel || %obj.DuelWait || %obj.InRace)
      return;
   //must be a fight to the death!  don't whimp out part way through!
   
   parent::doDismount(%this, %obj, %forced);


}
function SniperRifleImage::onFire(%data,%obj,%slot)
{
   if(%obj.onSnipingPad)
   {                 %obj.Client.SniperShots++;
         DisplaySniperScore(%obj.Client);
   }
   parent::onFire(%data, %obj, %slot);
}

function ProjectileData::onCollision(%data, %projectile, %targetObject, %modifier, %position, %normal)
{
	//if(isObject(%targetObject)) // Console spam fix.
 //   {
 //      if(%targetObject.inDuel)
 //      {

 //           if(Duels.Duel[%targetObject.DuelID].Type $= "MA Disk" && !IsMidAirHight(%targetObject))
 //           {
 //               error("It was't an MA... returning");
 //                return;
//            }
//       }
	//}

    parent::onCollision( %data, %projectile, %targetObject, %modifier, %position, %normal );
}
function Armor::onCollision(%this,%obj,%col,%forceVehicleNode)
{

   if (%obj.getState() $= "Dead")
      return;

   %dataBlock = %col.getDataBlock();
   %className = %dataBlock.className;
   %client = %obj.client;
   // player collided with a vehicle
   %node = -1;
   if (%forceVehicleNode !$= "" || (%className $= WheeledVehicleData || %className $= FlyingVehicleData || %className $= HoverVehicleData) &&
         %obj.mountVehicle && %obj.getState() $= "Move" && %col.mountable && !%obj.inStation && %col.getDamageState() !$= "Destroyed") {

      //if the player is an AI, he should snap to the mount points in node order,
      //to ensure they mount the turret before the passenger seat, regardless of where they collide...
      if (%obj.client.isAIControlled())
      {
         %transform = %col.getTransform();

         //either the AI is *required* to pilot, or they'll pick the first available passenger seat
         if (%client.pilotVehicle)
         {
            //make sure the bot is in light armor
            if (%client.player.getArmorSize() $= "Light")
            {
               //make sure the pilot seat is empty
               if (!%col.getMountNodeObject(0))
                  %node = 0;
            }
         }
         else
            %node = findAIEmptySeat(%col, %obj);
      }
      else
         %node = findEmptySeat(%col, %obj, %forceVehicleNode);

      //now mount the player in the vehicle
      if(%node >= 0)
      {
         // players can't be pilots, bombardiers or turreteers if they have
         // "large" packs -- stations, turrets, turret barrels
         if(hasLargePack(%obj)) {
            // check to see if attempting to enter a "sitting" node
            if(nodeIsSitting(%datablock, %node)) {
               // send the player a message -- can't sit here with large pack
               if(!%obj.noSitMessage)
               {
                  %obj.noSitMessage = true;
                  %obj.schedule(2000, "resetSitMessage");
                  messageClient(%obj.client, 'MsgCantSitHere', '\c2Pack too large, can\'t occupy this seat.~wfx/misc/misc.error.wav');
               }
               return;
            }
         }
         if(%col.noEnemyControl && %obj.team != %col.team)
            return;

         commandToClient(%obj.client,'SetDefaultVehicleKeys', true);
         //If pilot or passenger then bind a few extra keys
         if(%node == 0)
            commandToClient(%obj.client,'SetPilotVehicleKeys', true);
         else
            commandToClient(%obj.client,'SetPassengerVehicleKeys', true);

         if(!%obj.inStation)
            %col.lastWeapon = ( %col.getMountedImage($WeaponSlot) == 0 ) ? "" : %col.getMountedImage($WeaponSlot).getName().item;
         else
            %col.lastWeapon = %obj.lastWeapon;

         %col.mountObject(%obj,%node);
         %col.playAudio(0, MountVehicleSound);
         %obj.mVehicle = %col;

			// if player is repairing something, stop it
			if(%obj.repairing)
				stopRepairing(%obj);

         //this will setup the huds as well...
         %dataBlock.playerMounted(%col,%obj, %node);
      }
   }
   else if (%className $= "Armor") {
      // player has collided with another player
      if(%col.getState() $= "Dead" && !%col.InDuel) {
         %gotSomething = false;
         // it's corpse-looting time!
         // weapons -- don't pick up more than you are allowed to carry!
         for(%i = 0; ( %obj.weaponCount < %obj.getDatablock().maxWeapons ) && $InvWeapon[%i] !$= ""; %i++)
         {
            %weap = $NameToInv[$InvWeapon[%i]];
            if ( %col.hasInventory( %weap ) )
            {
               if ( %obj.incInventory(%weap, 1) > 0 )
               {
                  %col.decInventory(%weap, 1);
                  %gotSomething = true;
                  messageClient(%obj.client, 'MsgItemPickup', '\c0You picked up %1.', %weap.pickUpName);
               }
            }
         }
         // targeting laser:
         if ( %col.hasInventory( "TargetingLaser" ) )
         {
            if ( %obj.incInventory( "TargetingLaser", 1 ) > 0 )
            {
               %col.decInventory( "TargetingLaser", 1 );
               %gotSomething = true;
               messageClient( %obj.client, 'MsgItemPickup', '\c0You picked up a targeting laser.' );
            }
         }
         // ammo
         for(%j = 0; $ammoType[%j] !$= ""; %j++)
         {
            %ammoAmt = %col.inv[$ammoType[%j]];
            if(%ammoAmt)
            {
               // incInventory returns the amount of stuff successfully grabbed
               %grabAmt = %obj.incInventory($ammoType[%j], %ammoAmt);
               if(%grabAmt > 0)
               {
                  %col.decInventory($ammoType[%j], %grabAmt);
                  %gotSomething = true;
                  messageClient(%obj.client, 'MsgItemPickup', '\c0You picked up %1.', $ammoType[%j].pickUpName);
                  %obj.client.setWeaponsHudAmmo($ammoType[%j], %obj.getInventory($ammoType[%j]));
               }
            }
         }
         // figure out what type, if any, grenades the (live) player has
         %playerGrenType = "None";
         for(%x = 0; $InvGrenade[%x] !$= ""; %x++) {
            %gren = $NameToInv[$InvGrenade[%x]];
            %playerGrenAmt = %obj.inv[%gren];
            if(%playerGrenAmt > 0)
            {
               %playerGrenType = %gren;
               break;
            }
         }
         // grenades
         for(%k = 0; $InvGrenade[%k] !$= ""; %k++)
         {
            %gren = $NameToInv[$InvGrenade[%k]];
            %corpseGrenAmt = %col.inv[%gren];
            // does the corpse hold any of this grenade type?
            if(%corpseGrenAmt)
            {
               // can the player pick up this grenade type?
               if((%playerGrenType $= "None") || (%playerGrenType $= %gren))
               {
                  %taken = %obj.incInventory(%gren, %corpseGrenAmt);
                  if(%taken > 0)
                  {
                     %col.decInventory(%gren, %taken);
                     %gotSomething = true;
                     messageClient(%obj.client, 'MsgItemPickup', '\c0You picked up %1.', %gren.pickUpName);
                     %obj.client.setInventoryHudAmount(%gren, %obj.getInventory(%gren));
                  }
               }
               break;
            }
         }
         // figure out what type, if any, mines the (live) player has
         %playerMineType = "None";
         for(%y = 0; $InvMine[%y] !$= ""; %y++)
         {
            %mType = $NameToInv[$InvMine[%y]];
            %playerMineAmt = %obj.inv[%mType];
            if(%playerMineAmt > 0)
            {
               %playerMineType = %mType;
               break;
            }
         }
         // mines
         for(%l = 0; $InvMine[%l] !$= ""; %l++)
         {
            %mine = $NameToInv[$InvMine[%l]];
            %mineAmt = %col.inv[%mine];
            if(%mineAmt) {
               if((%playerMineType $= "None") || (%playerMineType $= %mine))
               {
                  %grabbed = %obj.incInventory(%mine, %mineAmt);
                  if(%grabbed > 0)
                  {
                     %col.decInventory(%mine, %grabbed);
                     %gotSomething = true;
                     messageClient(%obj.client, 'MsgItemPickup', '\c0You picked up %1.', %mine.pickUpName);
                     %obj.client.setInventoryHudAmount(%mine, %obj.getInventory(%mine));
                  }
               }
               break;
            }
         }
         // beacons
         %beacAmt = %col.inv[Beacon];
         if(%beacAmt)
         {
            %bTaken = %obj.incInventory(Beacon, %beacAmt);
            if(%bTaken > 0)
            {
               %col.decInventory(Beacon, %bTaken);
               %gotSomething = true;
               messageClient(%obj.client, 'MsgItemPickup', '\c0You picked up %1.', Beacon.pickUpName);
               %obj.client.setInventoryHudAmount(Beacon, %obj.getInventory(Beacon));
            }
         }
         // repair kit
         %rkAmt = %col.inv[RepairKit];
         if(%rkAmt)
         {
            %rkTaken = %obj.incInventory(RepairKit, %rkAmt);
            if(%rkTaken > 0)
            {
               %col.decInventory(RepairKit, %rkTaken);
               %gotSomething = true;
               messageClient(%obj.client, 'MsgItemPickup', '\c0You picked up %1.', RepairKit.pickUpName);
               %obj.client.setInventoryHudAmount(RepairKit, %obj.getInventory(RepairKit));
            }
         }
      }
      if(%gotSomething)
         %col.playAudio(0, CorpseLootingSound);
   }
}



function ELFProjectileData::zapTarget(%data, %projectile, %target, %targeter)
{
    if(%target.InNoFireZone || %target.onSnipingPad || %target.Duelwait
                            || %target.inRace || %targeter.InNoFireZone
                            || %targeter.OnSipingPad || %targeter.Duelwait
                            || %target.inRace)
    {
       //Fix infinite energy bug
       %target.BadZap = true;
       return;
    }
    %target.BadZap = false;
    if(%target.InDuel || %targeter.InDuel)
    {
        if(!%target.InDuel || !%targeter.InDuel || %target.DuelID != %targeter.DuelID)
           return;
    
    }
    %oldERate = %target.getRechargeRate();
	%target.teamDamageStateOnZap = $teamDamage;
   %teammates = %target.client.team == %targeter.client.team;

	if( %target.teamDamageStateOnZap || !%teammates )
		%target.setRechargeRate(%oldERate - %data.drainEnergy);
	else
		%target.setRechargeRate(%oldERate);

	%projectile.checkELFStatus(%data, %target, %targeter);
}
function ELFProjectileData::unzapTarget(%data, %projectile, %target, %targeter)
{
    //fix infinite energy bug
    if(%target.BadZap)
       return;
    cancel(%projectile.ELFrecur);
	%target.stopAudio($ELFZapSound);
	%targeter.stopAudio($ELFFireSound);
	%target.zapSound = false;
	%targeter.zappingSound = false;
   %teammates = %target.client.team == %targeter.client.team;

	if(!%target.isDestroyed())
	{
		%oldERate = %target.getRechargeRate();
		if( %target.teamDamageStateOnZap || !%teammates )
			%target.setRechargeRate(%oldERate + %data.drainEnergy);
		else
			%target.setRechargeRate(%oldERate);
	}
}


function Player::use( %this,%data )
{
   //Kraska Edit:: I had to make it so i could switch
   //weapons in the sniper station

   // If player is in a station then he can't use any items
   if(%this.station !$= "" && !%this.onSnipingPad)
      return false;

   // Convert the word "Backpack" to whatever is in the backpack slot.
   if ( %data $= "Backpack" )
   {
      if ( %this.inStation )
         return false;

      if ( %this.isPilot() )
      {
         messageClient( %this.client, 'MsgCantUsePack', '\c2You can\'t use your pack while piloting.~wfx/misc/misc.error.wav' );
         return( false );
      }
      else if ( %this.isWeaponOperator() )
      {
         messageClient( %this.client, 'MsgCantUsePack', '\c2You can\'t use your pack while in a weaponry position.~wfx/misc/misc.error.wav' );
         return( false );
      }

      %image = %this.getMountedImage( $BackpackSlot );
      if ( %image )
         %data = %image.item;
   }

   // Can't use some items when piloting or your a weapon operator
   if ( %this.isPilot() || %this.isWeaponOperator() )
      if ( %data.getName() !$= "RepairKit" )
         return false;

   return ShapeBase::use( %this, %data );
}



function AIThrowObject(%object)
{
	//$AIItemSet.add(%object);
}
function AICorpseAdded(%corpse)
{
//  avoid spam
}
function Armor::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType, %momVec, %mineSC)
{
  /////////////////////////////////////////////////////////
  //Race Data
  /////////////////////////////////////////////////////////
      if((%targetObject.inRace || %sourceObject.inRace) &&
                      %targetObject != %sourceObject &&
                      %damageType != $DamageType::Suicide &&
                      %damageType != $DamageType::Ground &&
                      %damageType != $DamageType::Impact &&
                      %damageType != $DamageType::Default )
      return;

  /////////////////////////////////////////////////////////
  //NO Fire Zone Data
  /////////////////////////////////////////////////////////
     if(%sourceObject.InNoFireZone && %damageType != $DamageType::ELF)
   {
      error(%damagetype);
      TGGame::Punish(%sourceObject);
      if(!%sourceObject.Client.NoFireWarnWait)
         {
         messageClient(%sourceObject.client, 'ChatMessage', '\c5DO NOT fire while in the no fire zone~wfx/misc/lightning_impact.wav');
         %sourceObject.Client.NoFireWarnWait = true;
         schedule(1000, 0, "NoFireWarnWait", %sourceObject.client);
         }
      return;
   }
   if(%targetObject.InNoFireZone && %damageType != $DamageType::Suicide
                                 && %damageType != $DamageType::Default
                                 && %damageType != $DamageType::ELF)
   {

      TGGame::Punish(%sourceObject);
      if(!%sourceObject.Client.NoFireWarnWait)
      {
         messageClient(%sourceObject.client, 'ChatMessage', '\c5DO NOT FIRE fire on those within the no fire zone~wfx/misc/lightning_impact.wav');
         %sourceObject.Client.NoFireWarnWait = true;
         schedule(1000, 0, "NoFireWarnWait", %sourceObject.client);
         }
         return;
   }
  /////////////////////////////////////////////////////////
  //Snipe Pad Data
  /////////////////////////////////////////////////////////

    if((%targetObject.onSnipingPad || %sourceObject.onSnipingPad) && %damageType != $DamageType::Suicide
                                                                 && %damageType != $DamageType::Default )
    {
      if(%targetObject.onSnipingPad)
      {
         TGGame::Punish(%sourceObject);
         if(!%sourceObject.Client.NoFireWarnWait)
         {
            messageClient(%sourceObject.client, 'ChatMessage', '\c5Do NOT be a jerk, Thanks.~wfx/misc/lightning_impact.wav');
            %sourceObject.Client.NoFireWarnWait = true;
            schedule(1000, 0, "NoFireWarnWait", %sourceObject.client);
         }
      }

      return;
     }

  /////////////////////////////////////////////////////////
  //Duel Data
  /////////////////////////////////////////////////////////
    if(%targetObject.Duelwait || %sourceObject.Duelwait)
       return;

    // make sure the dueler can't be hurt by the outside world...
    if((%targetObject.InDuel || %SourceObject.InDuel) && %damagetype != $DamageType::Suicide
                             && %damagetype != 0  && %damagetype != $DamageType::Ground
                             && %damagetype != $DamageType::Impact)
                             
    {
       if(!%sourceObject.InDuel || !%targetObject.InDuel || %sourceObject.DuelID != %targetObject.DuelID)
          return;

       if(Duels.Duel[%targetObject.DuelID].Type $= "MA Disk" && !IsMidAirHight(%targetObject))
            {
                error("It was't an MA... returning");
                 return;
            }
    }

   messageClient(%sourceObject.client, 'ChatMessage', '~wgui/Objective_notification.wav');
   parent::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType, %momVec, %mineSC);
}
function VehicleData::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType, %momVec, %theClient, %proj)
{
   if(%targetObject.Duelwait || %sourceObject.Duelwait)
       return;
   if((%targetObject.InDuel || %SourceObject.InDuel) && %damagetype != $DamageType::Suicide
                             && %damagetype != 0  && %damagetype != $DamageType::Ground
                             && %damagetype != $DamageType::Impact)

    {
       if(!%sourceObject.InDuel || !%targetObject.InDuel || %sourceObject.DuelID != %targetObject.DuelID)
          return;
    }
   parent::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType, %momVec, %theClient, %proj);
}
function VehicleData::onImpact(%data, %vehicleObject, %collidedObject, %vec, %vecLen)
{
   if(%vehicleObject.Duelwait || %collidedObject.Duelwait)
       return;
   if(%collidedObject.InDuel || %vehicleObject.InDuel)
       if(!%vehicleObject.InDuel || !%collidedObject.InDuel || %vehicleObject.DuelID != %collidedObject.DuelID)
          return;

  parent::onImpact(%data, %vehicleObject, %collidedObject, %vec, %vecLen);
}
function VehicleData::onDestroyed(%data, %obj, %prevState)
{
   //Just make sure everyone onboard dies so the default Duel::onClientKilled
   //is called
   if(%obj.inDuel || %obj.inRace)
   {
      for(%i = 0; %i < %obj.getDatablock().numMountPoints; %i++)
      {
         if (%obj.getMountNodeObject(%i))
         {
            %player = %obj.getMountNodeObject(%i);
            if(isobject(%Player))
               %player.Scriptkill(0);
         }
      }

   }
   parent::onDestroyed(%data, %obj, %prevState);
}
function ProjectileData::onExplode(%data, %proj, %pos, %mod)
{

   if (%data.hasDamageRadius)
      RadiusExplosion(%proj, %pos, %data.damageRadius, %data.indirectDamage, %data.kickBackStrength, %proj.sourceObject, %data.radiusDamageType);
}


function RadiusExplosion(%explosionSource, %position, %radius, %damage, %impulse, %sourceObject, %damageType)
{

   InitContainerRadiusSearch(%position, %radius, $TypeMasks::PlayerObjectType      |
                                                 $TypeMasks::VehicleObjectType     |
                                                 $TypeMasks::StaticShapeObjectType |
                                                 $TypeMasks::TurretObjectType      |
                                                 $TypeMasks::ItemObjectType);

   %numTargets = 0;
   while ((%targetObject = containerSearchNext()) != 0)
   {
      %dist = containerSearchCurrRadDamageDist();

      if (%dist > %radius)
         continue;

      if (%targetObject.isMounted())
      {
         %mount = %targetObject.getObjectMount();
         %found = -1;
         for (%i = 0; %i < %mount.getDataBlock().numMountPoints; %i++)
         {
            if (%mount.getMountNodeObject(%i) == %targetObject)
            {
               %found = %i;
               break;
            }
         }

         if (%found != -1)
         {
            if (%mount.getDataBlock().isProtectedMountPoint[%found])
            {
               continue;
            }
         }
      }

      %targets[%numTargets]     = %targetObject;
      %targetDists[%numTargets] = %dist;
      %numTargets++;
   }

   for (%i = 0; %i < %numTargets; %i++)
   {
      %targetObject = %targets[%i];
      %dist = %targetDists[%i];
      
//      if(%targetObject.client.TS == 0)
//        {
//        error("your trying to hit a innocent victim!");
//        continue;
//        }

      %coverage = calcExplosionCoverage(%position, %targetObject,
                                        ($TypeMasks::InteriorObjectType |
                                         $TypeMasks::TerrainObjectType |
                                         $TypeMasks::ForceFieldObjectType |
                                         $TypeMasks::VehicleObjectType));
      if (%coverage == 0)
         continue;

      if(%targetObject.InDuel || %SourceObject.InDuel)
    {
       if(!%sourceObject.InDuel || !%targetObject.InDuel || %sourceObject.DuelID != %targetObject.DuelID)
          return;


     // if(Duels.Duel[%targetObject.DuelID].Type $= "MA Disk" )
       //     {
       //         error("No Raidus Explosions for Mid Air type duel");
       //          return;
        //    }


    }


          //Kraska edit right here-------Make NO FIRE Zone work
          if(%sourceObject.InNoFireZone)
          {

              TGGame::Punish(%sourceObject);
              if(!%sourceObject.Client.NoFireWarnWait)
              {
                 messageClient(%sourceObject.client, 'ChatMessage', '\c5DO NOT fire while in the no fire zone~wfx/misc/lightning_impact.wav');
                 %sourceObject.Client.NoFireWarnWait = true;
                 schedule(1000, 0, "NoFireWarnWait",  %sourceObject.client);
              }
              return;
          }
          if(%targetObject.InNoFireZone)
          {

              TGGame::Punish(%sourceObject);
              if(!%sourceObject.Client.NoFireWarnWait)
              {
                 messageClient(%sourceObject.client, 'ChatMessage', '\c5DO NOT FIRE fire on those within the no fire zone~wfx/misc/lightning_impact.wav');
                 %sourceObject.Client.NoFireWarnWait = true;
                 schedule(1000, 0, "NoFireWarnWait", %sourceObject.client);
              }


              return;
          }
     //Kraska edit::  Protects racers from being bothered
//   if((%targetObject.Client.InRace || %sourceObject.Client.InRace) && %targetObject.client != %sourceObject.client)
//      return;


      //if ( $splashTest )
         %amount = (1.0 - ((%dist / %radius) * 0.88)) * %coverage * %damage;
      //else
         //%amount = (1.0 - (%dist / %radius)) * %coverage * %damage;

      //error( "damage: " @ %amount @ " at distance: " @ %dist @ " radius: " @ %radius @ " maxDamage: " @ %damage );

      %data = %targetObject.getDataBlock();
      %className = %data.className;

      if (%impulse && %data.shouldApplyImpulse(%targetObject))
      {
         %p = %targetObject.getWorldBoxCenter();
         %momVec = VectorSub(%p, %position);
         %momVec = VectorNormalize(%momVec);
         %impulseVec = VectorScale(%momVec, %impulse * (1.0 - (%dist / %radius)));
         %doImpulse = true;
      }
      else if( %className $= WheeledVehicleData || %className $= FlyingVehicleData || %className $= HoverVehicleData )
      {
         %p = %targetObject.getWorldBoxCenter();
         %momVec = VectorSub(%p, %position);
         %momVec = VectorNormalize(%momVec);

         %impulseVec = VectorScale(%momVec, %impulse * (1.0 - (%dist / %radius)));

         if( getWord( %momVec, 2 ) < -0.5 )
            %momVec = "0 0 1";

         // Add obj's velocity into the momentum vector
         %velocity = %targetObject.getVelocity();
         //%momVec = VectorNormalize( vectorAdd( %momVec, %velocity) );
         %doImpulse = true;
      }
      else
      {
         %momVec = "0 0 1";
         %doImpulse = false;
      }


          if(%amount > 0)
         %data.damageObject(%targetObject, %sourceObject, %position, %amount, %damageType, %momVec, %explosionSource.theClient, %explosionSource);
      else if( %explosionSource.getDataBlock().getName() $= "ConcussionGrenadeThrown" && %data.getClassName() $= "PlayerData" )
	  {
         %data.applyConcussion( %dist, %radius, %sourceObject, %targetObject );

 	  	if(!$teamDamage && %sourceObject != %targetObject && %sourceObject.client.team == %targetObject.client.team)
 	  	{
			messageClient(%targetObject.client, 'msgTeamConcussionGrenade', '\c1You were hit by %1\'s concussion grenade.', getTaggedString(%sourceObject.client.name));
		}
	  }

      if( %doImpulse )
         %targetObject.applyImpulse(%position, %impulseVec);
   }
}

function Armor::onTrigger(%data, %player, %triggerNum, %val)
{
   //trigger types:   0:fire 1:altTrigger 2:jump 3:jet 4:throw

   if(%player.client.inOptions)
   {
      if(%triggerNum == 0)
         IGOptions::End(%player.client, false);
      
      else if(%triggerNum == 3)
         if(%val == 1)
            IGOptions::DisplayCycle(%player.client);

      return;
   }
      if(%player.onSnipingPad)
   {
      if(%triggerNum == 3)
         if(%val == 1)
            SnipingPad::SpawnTarget(%player.client);
      if(%triggerNum == 2)
         SnipingPad::Exit(%player);
   }

 //------------Origionals below-----------------------------
   if (%triggerNum == 4)
   {
      // Throw grenade
      if (%val == 1)
      {
         %player.grenTimer = 1;
      }
      else
      {
         if (%player.grenTimer == 0)
         {
            // Bad throw for some reason
         }
         else
         {
            %player.use(Grenade);
            %player.grenTimer = 0;
         }
      }
   }
   else if (%triggerNum == 5)
   {
      // Throw mine
      if (%val == 1)
      {
         %player.mineTimer = 1;
      }
      else
      {
         if (%player.mineTimer == 0)
         {
            // Bad throw for some reason
         }
         else
         {
            %player.use(Mine);
            %player.mineTimer = 0;
         }
      }
   }
   else if (%triggerNum == 3)
   {
      // val = 1 when jet key (LMB) first pressed down
      // val = 0 when jet key released
      // MES - do we need this at all any more?
      if(%val == 1)
         %player.isJetting = true;
      else
         %player.isJetting = false;
   }
}

};
/////////////////////////////////////////////////////////////////////////////
//                          End of Package                                 //
/////////////////////////////////////////////////////////////////////////////
$TrainingStatus::Selecting = 0;
$TrainingStatus::Sniping = 1;
$TrainingStatus::Racing = 2;
$TrainingStatus::DeathMatch = 3;

function NoFireWarnWait(%client)
{

   %client.NoFireWarnWait = false;
}
function TGGame::displayDeathMessages(%game, %clVictim, %clKiller, %damageType, %implement)
{
   //Kraska Edit:: Removed the TK message part


   // ----------------------------------------------------------------------------------
   // z0dd - ZOD, 6/18/02. From Panama Jack, send the damageTypeText as the last varible
   // in each death message so client knows what weapon it was that killed them.

   %victimGender = (%clVictim.sex $= "Male" ? 'him' : 'her');
   %victimPoss = (%clVictim.sex $= "Male" ? 'his' : 'her');
   %killerGender = (%clKiller.sex $= "Male" ? 'him' : 'her');
   %killerPoss = (%clKiller.sex $= "Male" ? 'his' : 'her');
   %victimName = %clVictim.name;
   %killerName = %clKiller.name;
   //error("DamageType = " @ %damageType @ ", implement = " @ %implement @ ", implement class = " @ %implement.getClassName() @ ", is controlled = " @ %implement.getControllingClient());

   if(%damageType == $DamageType::Explosion)
   {
      messageAll('msgExplosionKill', $DeathMessageExplosion[mFloor(getRandom() * $DeathMessageExplosionCount)], %victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
      logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by a nearby explosion.");
   }
   else if(%damageType == $DamageType::Suicide)  //player presses cntrl-k
   {
      messageAll('msgSuicide', $DeathMessageSuicide[mFloor(getRandom() * $DeathMessageSuicideCount)], %victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
      logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") committed suicide (CTRL-K)");
   }
	else if(%damageType == $DamageType::VehicleSpawn)
	{
      messageAll('msgVehicleSpawnKill', $DeathMessageVehPad[mFloor(getRandom() * $DeathMessageVehPadCount)], %victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
      logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by vehicle spawn");
	}
	else if(%damageType == $DamageType::ForceFieldPowerup)
	{
      messageAll('msgVehicleSpawnKill', $DeathMessageFFPowerup[mFloor(getRandom() * $DeathMessageFFPowerupCount)], %victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
      logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by Force Field Powerup");
	}
	else if(%damageType == $DamageType::Crash)
	{
      messageAll('msgVehicleCrash', $DeathMessageVehicleCrash[%damageType, mFloor(getRandom() * $DeathMessageVehicleCrashCount)], %victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
      logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") crashes a vehicle.");
	}
	else if(%damageType == $DamageType::Impact) // run down by vehicle
	{
		if( ( %controller = %implement.getControllingClient() ) > 0)
		{
	      %killerGender = (%controller.sex $= "Male" ? 'him' : 'her');
	      %killerPoss = (%controller.sex $= "Male" ? 'his' : 'her');
	      %killerName = %controller.name;
			messageAll('msgVehicleKill', $DeathMessageVehicle[mFloor(getRandom() * $DeathMessageVehicleCount)], %victimName, %victimGender, %victimPoss, %killerName ,%killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
	      logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by a vehicle controlled by "@%controller);
		}
		else
		{
			messageAll('msgVehicleKill', $DeathMessageVehicleUnmanned[mFloor(getRandom() * $DeathMessageVehicleUnmannedCount)], %victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
	      logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by a vehicle (unmanned)");
		}
	}
   else if (isObject(%implement) && (%implement.getClassName() $= "Turret" || %implement.getClassName() $= "VehicleTurret" || %implement.getClassName() $= "FlyingVehicle" ))   //player killed by a turret
   {
      if (%implement.getControllingClient() != 0)  //is turret being controlled?
      {
         %controller = %implement.getControllingClient();
         %killerGender = (%controller.sex $= "Male" ? 'him' : 'her');
         %killerPoss = (%controller.sex $= "Male" ? 'his' : 'her');
         %killerName = %controller.name;

         if (%controller == %clVictim)
            messageAll('msgTurretSelfKill', $DeathMessageTurretSelfKill[mFloor(getRandom() * $DeathMessageTurretSelfKillCount)],%victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
         else if (%controller.team == %clVictim.team) //controller TK'd a friendly
            messageAll('msgCTurretKill', $DeathMessageCTurretTeamKill[%damageType, mFloor(getRandom() * $DeathMessageCTurretTeamKillCount)],%victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
         else //controller killed an enemy
            messageAll('msgCTurretKill', $DeathMessageCTurretKill[%damageType, mFloor(getRandom() * $DeathMessageCTurretKillCount)],%victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
         logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by a turret controlled by "@%controller);
      }
      // use the handle associated with the deployed object to verify valid owner
      else if (isObject(%implement.owner))
      {
         %owner = %implement.owner;
         //error("Owner is " @ %owner @ "   Handle is " @ %implement.ownerHandle);
         //error("Turret is still owned");
         //turret is uncontrolled, but is owned - treat the same as controlled.
         %killerGender = (%owner.sex $= "Male" ? 'him' : 'her');
         %killerPoss = (%owner.sex $= "Male" ? 'his' : 'her');
         %killerName = %owner.name;

         if (%owner.team == %clVictim.team)  //player got in the way of a teammates deployed but uncontrolled turret.
            messageAll('msgCTurretKill', $DeathMessageCTurretAccdtlKill[%damageType,mFloor(getRandom() * $DeathMessageCTurretAccdtlKillCount)],%victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
         else  //deployed, uncontrolled turret killed an enemy
            messageAll('msgCTurretKill', $DeathMessageCTurretKill[%damageType,mFloor(getRandom() * $DeathMessageCTurretKillCount)],%victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
         logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") was killed by turret (automated)");
      }
      else  //turret is not a placed (owned) turret (or owner is no longer on it's team), and is not being controlled
      {
         messageAll('msgTurretKill', $DeathMessageTurretKill[%damageType,mFloor(getRandom() * $DeathMessageTurretKillCount)],%victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
         logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by turret");
      }
   }
   else if((%clKiller == %clVictim) || (%damageType == $DamageType::Ground)) //player killed himself or fell to death
   {
      messageAll('msgSelfKill', $DeathMessageSelfKill[%damageType,mFloor(getRandom() * $DeathMessageSelfKillCount)], %victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
      logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed self ("@getTaggedString($DamageTypeText[%damageType])@")");
   }

   else if (%damageType == $DamageType::OutOfBounds) //killer died due to Out-of-Bounds damage
   {
      messageAll('msgOOBKill', $DeathMessageOOB[mFloor(getRandom() * $DeathMessageOOBCount)], %victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
      logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by out-of-bounds damage");
   }

   else if (%damageType == $DamageType::NexusCamping) //Victim died from camping near the nexus...
   {
      messageAll('msgCampKill', $DeathMessageCamping[mFloor(getRandom() * $DeathMessageCampingCount)], %victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
      logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed for nexus camping");
   }

//   else if(%clKiller.team == %clVictim.team) //was a TK
//   {
//      messageAll('msgTeamKill', $DeathMessageTeamKill[%damageType, mFloor(getRandom() * $DeathMessageTeamKillCount)],  %victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
//      logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") teamkilled by "@%clKiller.nameBase@" (pl "@%clKiller.player@"/cl "@%clKiller@")");
//   }

   else if (%damageType == $DamageType::Lava)   //player died by falling in lava
   {
      messageAll('msgLavaKill',  $DeathMessageLava[mFloor(getRandom() * $DeathMessageLavaCount)], %victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
      logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by lava");
   }
   else if ( %damageType == $DamageType::Lightning )  // player was struck by lightning
   {
      messageAll('msgLightningKill',  $DeathMessageLightning[mFloor(getRandom() * $DeathMessageLightningCount)], %victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
      logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by lightning");
   }
   else if ( %damageType == $DamageType::Mine && !isObject(%clKiller) )
   {
         error("Mine kill w/o source");
         messageAll('MsgRogueMineKill', $DeathMessageRogueMine[%damageType, mFloor(getRandom() * $DeathMessageRogueMineCount)], %victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
   }
   else  //was a legitimate enemy kill
   {
      if(%damageType == 6 && (%clVictim.headShot))
      {
         // laser headshot just occurred
         messageAll('MsgHeadshotKill', $DeathMessageHeadshot[%damageType, mFloor(getRandom() * $DeathMessageHeadshotCount)], %victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);

      }
      else
         messageAll('MsgLegitKill', $DeathMessage[%damageType, mFloor(getRandom() * $DeathMessageCount)], %victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
      logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by "@%clKiller.nameBase@" (pl "@%clKiller.player@"/cl "@%clKiller@") using "@getTaggedString($DamageTypeText[%damageType]));
   }
}


function TGGame::allowsProtectedStatics(%game)
{
   return true;
}
function TGGame::Punish(%player)
{
      if(!isobject(%player) || %player.punishing)
         return;

      %player.punishing = true;
      %damloc = "legs front_right";
      %player.blowup();
      Game.onClientKilled(%player.client, %player.client, 0, %player, %damLoc);
      %player.schedule(50, "delete");
}

function TGGame::updateKillScores(%game, %clVictim, %clKiller, %damageType, %implement)
{
// i'll work with this later
    %game.awardScoreKill(%clKiller);
    error("awarding kill score");
}


function TGGame::onClientDamaged(%game, %clVictim, %clAttacker, %damageType, %sourceObject)
{
   DefaultGame::onClientDamaged(%game, %clVictim, %clAttacker, %damageType, %sourceObject);
}

function TGGame::equip(%game, %player)
{

   for(%i =0; %i<$InventoryHudCount; %i++)
      %player.client.setInventoryHudItem($InventoryHudData[%i, itemDataName], 0, 1);
   %player.client.clearBackpackIcon();

//   %player.setArmor("Medium");
   %player.setInventory(RepairKit,1);

   %player.setInventory(EnergyPack,1);
   %player.setInventory(Beacon, 3);
   %player.setInventory(TargetingLaser, 1);
   %player.weaponCount = 0;
   %player.use("TargetingLaser");

   %player.setInventory(Disc,1);
   %player.setInventory(Discammo,15);

   %player.client.TS = $TrainingStatus::Selecting;
}
function TGGame::onClientKilled(%game, %clVictim, %clKiller, %damageType, %implement, %damageLocation)
{
    error("TGGame::onclient killed called");
   if(%clVictim.player.inDuel)
      Duels.onClientKilled(%clVictim, %clKiller, %dammageType);
   if(%clVictim.inoptions)
   {
    %clVictim.inoptions = false;
    clearCenterPrint( %clVictim );
   }
   if(%clVictim.player.InRace)
   {
     %race = %clVictim.player.Race;
     %race.RemovePlayer(%clVictim.player);
     // $Race::freePadID[%clVictim.RaceGroup, $Race::freePadCount[%clVictim.RaceGroup]] = %clVictim.RacePad;
     // $Race::freePadCount[%clVictim.RaceGroup]++;
     // Race::RemoveRacer(%clVictim);
     // clearCenterPrint( %clVictim );
   }
   %plVictim = %clVictim.player;
   %plKiller = %clKiller.player;
   %clVictim.plyrPointOfDeath = %plVictim.position;
   %clVictim.plyrDiedHoldingFlag = %plVictim.holdingFlag;
   %clVictim.waitRespawn = 1;

   cancel( %plVictim.reCloak );
   cancel(%clVictim.respawnTimer);
   %clVictim.respawnTimer = %game.schedule(($Host::PlayerRespawnTimeout * 1000), "forceObserver", %clVictim, "spawnTimeout" );

   // reset the alarm for out of bounds
   if(%clVictim.outOfBounds)
      messageClient(%clVictim, 'EnterMissionArea', "");

   if (%damageType == $DamageType::suicide)
      {
         if(%clVictim.TS == $TrainingStatus::Selecting)
            %respawnDelay = 0.5;
         else
            %respawnDelay = 10;
      
      }
   else
      %respawnDelay = 2;


   %game.schedule(%respawnDelay*1000, "clearWaitRespawn", %clVictim);
   // if victim had an undetonated satchel charge pack, get rid of it
   if(%plVictim.thrownChargeId != 0)
      if(!%plVictim.thrownChargeId.kaboom)
         %plVictim.thrownChargeId.delete();

   if(%plVictim.lastVehicle !$= "")
   {
      schedule(15000, %plVictim.lastVehicle,"vehicleAbandonTimeOut", %plVictim.lastVehicle);
      %plVictim.lastVehicle.lastPilot = "";
   }

   // unmount pilot or remove sight from bomber
   if(%plVictim.isMounted())
   {
      if(%plVictim.vehicleTurret)
         %plVictim.vehicleTurret.getDataBlock().playerDismount(%plVictim.vehicleTurret);
      else
      {
         %plVictim.getDataBlock().doDismount(%plVictim, true);
         %plVictim.mountVehicle = false;
      }
   }

   if(%plVictim.inStation)
      commandToClient(%plVictim.client,'setStationKeys', false);
   %clVictim.camera.mode = "playerDeath";

   // reset who triggered this station and cancel outstanding armor switch thread
   if(%plVictim.station)
   {
      %plVictim.station.triggeredBy = "";
      %plVictim.station.getDataBlock().stationTriggered(%plVictim.station,0);
      if(%plVictim.armorSwitchSchedule)
         cancel(%plVictim.armorSwitchSchedule);
   }

   //Close huds if player dies...
   messageClient(%clVictim, 'CloseHud', "", 'inventoryScreen');
   messageClient(%clVictim, 'CloseHud', "", 'vehicleHud');
   commandToClient(%clVictim, 'setHudMode', 'Standard', "", 0);

   // $weaponslot from item.cs
   %plVictim.setRepairRate(0);
   %plVictim.setImageTrigger($WeaponSlot, false);

   playDeathAnimation(%plVictim, %damageLocation, %damageType);
   playDeathCry(%plVictim);

   %victimName = %clVictim.name;

   %game.displayDeathMessages(%clVictim, %clKiller, %damageType, %implement);
   %game.updateKillScores(%clVictim, %clKiller, %damageType, %implement);

   // toss whatever is being carried, '$flagslot' from item.cs
   // MES - had to move this to after death message display because of Rabbit game type
   for(%index = 0 ; %index < 8; %index++)
   {
      %image = %plVictim.getMountedImage(%index);
      if(%image)
      {
         if(%index == $FlagSlot)
            %plVictim.throwObject(%plVictim.holdingFlag);
         else
            %plVictim.throw(%image.item);
      }
   }

   // target manager update
   setTargetDataBlock(%clVictim.target, 0);
   setTargetSensorData(%clVictim.target, 0);

   // clear the hud
   %clVictim.SetWeaponsHudClearAll();
   %clVictim.SetInventoryHudClearAll();
   %clVictim.setAmmoHudCount(-1);

   // clear out weapons, inventory and pack huds
   messageClient(%clVictim, 'msgDeploySensorOff', "");  //make sure the deploy hud gets shut off
   messageClient(%clVictim, 'msgPackIconOff', "");  // clear the pack icon

   //clear the deployable HUD
   %plVictim.client.deployPack = false;
   cancel(%plVictim.deployCheckThread);
   deactivateDeploySensor(%plVictim);

   //if the killer was an AI...
   if (isObject(%clKiller) && %clKiller.isAIControlled())
      %game.onAIKilledClient(%clVictim, %clKiller, %damageType, %implement);


   // reset control object on this player: also sets 'playgui' as content
   serverCmdResetControlObject(%clVictim);

   // set control object to the camera
   %clVictim.player = 0;
   %transform = %plVictim.getTransform();

   //note, AI's don't have a camera...
   if (isObject(%clVictim.camera))
   {
      %clVictim.camera.setTransform(%transform);
      %clVictim.camera.setOrbitMode(%plVictim, %plVictim.getTransform(), 0.5, 4.5, 4.5);
      %clVictim.setControlObject(%clVictim.camera);
   }

   //hook in the AI specific code for when a client dies
   if (%clVictim.isAIControlled())
   {
      aiReleaseHumanControl(%clVictim.controlByHuman, %clVictim);
      %game.onAIKilled(%clVictim, %clKiller, %damageType, %implement);
   }
   else
      aiReleaseHumanControl(%clVictim, %clVictim.controlAI);

   //used to track corpses so the AI can get ammo, etc...
   AICorpseAdded(%plVictim);

   //if the death was a suicide, prevent respawning for 5 seconds...
   %clVictim.lastDeathSuicide = false;
   if (%damageType == $DamageType::Suicide)
   {
      %clVictim.lastDeathSuicide = true;
      %clVictim.suicideRespawnTime = getSimTime() + 5000;
   }



}

function TGGame::assignClientTeam(%game, %client, %respawn )
{
// Hack ... just make em join team 1  -kraska
   %client.team = 1;
   %client.lastTeam = 1;

// Assign the team skin:
   setTargetSkin( %client.target, %game.getTeamSkin(%client.team) );

   messageAllExcept( %client, -1, 'MsgClientJoinTeam', '\c1%1 joined %2.', %client.name, %game.getTeamName(%client.team), %client, %client.team );
   messageClient( %client, 'MsgClientJoinTeam', '\c1You joined the %2 team.', %client.name, %game.getTeamName(%client.team), %client, %client.team );

   updateCanListenState( %client );

   logEcho(%client.nameBase@" (cl "@%client@") joined team "@%client.team);
}
function TGGame::createPlayer(%game, %client, %spawnLoc, %respawn)
{

   // do not allow a new player if there is one (not destroyed) on this client
   if(isObject(%client.player) && (%client.player.getState() !$= "Dead"))
      return;

   // clients and cameras can exist in team 0, but players should not
   if(%client.team == 0)
      error("Players should not be added to team0!");

   // defaultplayerarmor is in 'players.cs'
   if(%spawnLoc == -1)
      %spawnLoc = "0 0 300 1 0 0 0";
   //else
   //  echo("Spawning player at " @ %spawnLoc);

   // copied from player.cs
   if (%client.race $= "Bioderm")
      // Only have male bioderms.
      %armor = $DefaultPlayerArmor @ "Male" @ %client.race @ Armor;
   else
      %armor = $DefaultPlayerArmor @ %client.sex @ %client.race @ Armor;
   %client.armor = $DefaultPlayerArmor;

   %player = new Player() {
      //dataBlock = $DefaultPlayerArmor;
      dataBlock = %armor;
   };


   if(%respawn)
   {
      %player.setInvincible(true);
      %player.setCloaked(true);
      %player.setInvincibleMode($InvincibleTime,0.02);
      %player.respawnCloakThread = %player.schedule($InvincibleTime * 1000, "setRespawnCloakOff");
      %player.schedule($InvincibleTime * 1000, "setInvincible", false);
   }

   %player.setTransform( %spawnLoc );
   MissionCleanup.add(%player);

   // setup some info
   %player.setOwnerClient(%client);
   %player.team = %client.team;
   %client.outOfBounds = false;
   %player.setEnergyLevel(60);
   %client.player = %player;

   // updates client's target info for this player
   %player.setTarget(%client.target);
   setTargetDataBlock(%client.target, %player.getDatablock());
   setTargetSensorData(%client.target, PlayerSensor);
   setTargetSensorGroup(%client.target, %client.team);
   %client.setSensorGroup(%client.team);

   //make sure the player has been added to the team rank array...
   %game.populateTeamRankArray(%client);

   %game.playerSpawned(%client.player);

   %client.TS = $TrainingStatus::Selecting;
   
   //Set the default sensor group
   %client.SetSensorGroup(1);
}
function TGGame::initGameVars(%game)
{
    %game.SCORE_PER_SUICIDE = 0;
   %game.SCORE_PER_TEAMKILL = 10;
   %game.SCORE_PER_DEATH = -10;

   %game.SCORE_PER_KILL = 10;

   %game.SCORE_PER_TURRET_KILL = 0;

}

function TGGame::endMission( %game )
{
  // error("calling TGGame::endMission");
   $TSSelector::Count = 0;
   races.clear();
 //  for(%i = 0; %i <= $Race::HighestGroup; %i++)
 //  {
 //  $Race::padCount[%i] = 0;
 //  $Race::freePadCount[%i] = 0;
   
 //  }
   for(%i = 1; %i <= 4; %i++)
  {
      for(%j = 1; %j <= $DuelBlock::GroupCount[%i]; %j++)
      {
       $DuelBlock::GroupInUse[%i, %j] = false;
      }
      $DuelBlock::GroupCount[%i] = 0;
      
  }
  //hack ... may cause problems if over 100 groups
  //[EDIT] : 43 Groups is all i should EVER need... but i'll leave it at 100
  //[EDIT] I need even less for the duel stations.... but oh well
  for(%i = 0; %i <= 100; %i++)
  {
     $DuelBlock::BlockCount[%i] = 0;
     $DuelStation::GroupCount[%i] = 0;
  }

}
function TGGame::timeLimitReached(%game)
{
   logEcho("game over (timelimit)");
   %game.gameOver();
   cycleMissions();
}
function TGGame::clientJoinTeam( %game, %client, %team, %respawn )
{
//error("DefaultGame::clientJoinTeam");
   if ( %team < 1 || %team > %game.numTeams )
      return;

   if( %respawn $= "" )
      %respawn = 1;

   %client.team = %team;
   %client.lastTeam = %team;
   setTargetSkin( %client.target, %game.getTeamSkin(%team) );
   setTargetSensorGroup( %client.target, %team );
   %client.setSensorGroup( %team );

   // Spawn the player:
   %game.spawnPlayer( %client, %respawn );

   messageAllExcept( %client, -1, 'MsgClientJoinTeam', '\c1%1 joined %2.', %client.name, %game.getTeamName(%team), %client, %team );
   messageClient( %client, 'MsgClientJoinTeam', '\c1You joined the %2 team.', $client.name, %game.getTeamName(%client.team), %client, %client.team );

   updateCanListenState( %client );

   logEcho(%client.nameBase@" (cl "@%client@") joined team "@%client.team);
}
function TGGame::gameOver(%game)
{
  DefaultGame::gameOver(%game);
  messageAll('MsgGameOver', "Match has ended.~wvoice/announcer/ann.gameover.wav" );

  //Fix a bug that occures if a race is inprogress while ending
  for(%i = 0; %i <= $Race::MaxGroup; %i++)
  {
     if($Race::Exists[%i])
        Race::ClearRace(%i);
  }
}
function TGGame::leaveMissionArea(%game, %playerData, %player)
{
  // I don't want them to bother with out of bounds
}

function TGGame::enterMissionArea(%game, %playerData, %player)
{
// I don't want them to bother with out of bounds
}
function TGGame::clientMissionDropReady(%game, %client)
{
   messageClient(%client, 'MsgClientReady',"", %game.class);
   %game.resetScore(%client);
   for(%i = 1; %i <= %game.numTeams; %i++)
   {
      $Teams[%i].score = 0;
      messageClient(%client, 'MsgCTFAddTeam', "", %i, %game.getTeamName(%i), $flagStatus[%i], $TeamScore[%i]);
   }
   //%game.populateTeamRankArray(%client);

   //messageClient(%client, 'MsgYourRankIs', "", -1);

   messageClient(%client, 'MsgMissionDropInfo', '\c0You are in mission %1 (%2).', $MissionDisplayName, $MissionTypeDisplayName, $ServerName );

   DefaultGame::clientMissionDropReady(%game, %client);
}
function TGGame::setUpTeams(%game)
{
//Use the DefaultGame version
%group = nameToID("MissionGroup/Teams");
   if(%group == -1)
      return;

   // create a team0 if it does not exist
   %team = nameToID("MissionGroup/Teams/team0");
   if(%team == -1)
   {
      %team = new SimGroup("team0");
      %group.add(%team);
   }

   // 'team0' is not counted as a team here
   %game.numTeams = 0;
   while(%team != -1)
   {
      // create drop set and add all spawnsphere objects into it
      %dropSet = new SimSet("TeamDrops" @ %game.numTeams);
      MissionCleanup.add(%dropSet);

      %spawns = nameToID("MissionGroup/Teams/team" @ %game.numTeams @ "/SpawnSpheres");
      if(%spawns != -1)
      {
         %count = %spawns.getCount();
         for(%i = 0; %i < %count; %i++)
            %dropSet.add(%spawns.getObject(%i));
      }

      // set the 'team' field for all the objects in this team
      %team.setTeam(%game.numTeams);

      clearVehicleCount(%team+1);
      // get next group
      %team = nameToID("MissionGroup/Teams/team" @ %game.numTeams + 1);
      if (%team != -1)
         %game.numTeams++;
   }

   // set the number of sensor groups (including team0) that are processed
   %MaxS = Duels.maxSensorGroup;
   setSensorGroupCount(%MaxS);
   error("Setting up my sensor groups");
   //Set up the invincible White Group and how they see everyone
   SetSensorGroupColor(0,~0,"0 0 0 255");         //All Duelers - Black
   SetSensorGroupColor(0,1<<0,"255 255 255 255"); //Same Group - White
   SetSensorGroupColor(0,1<<1,"255 0 0 255");     //Death Match - Red
   SetSensorGroupColor(0,1<<2,"0 0 255 255");
   SetSensorGroupColor(0,1<<3,"255 255 10 100");     //Racers - Blue
   
   //Set up Standard DeathMatch
   SetSensorGroupColor(1,~0,"0 0 0 255");         //All Duelers - Black
   SetSensorGroupColor(1,1<<0,"255 255 255 255"); //Invincible - White
   SetSensorGroupColor(1,1<<1,"255 0 0 255");     //Death Match - Red
   SetSensorGroupColor(1,1<<2,"0 0 255 255");
   SetSensorGroupColor(1,1<<3,"255 255 10 100");     //Racers - Blue
   
   //Setup Racers
   SetSensorGroupColor(2,~0,"255 255 255 255");   //Everyone Else - White
   SetSensorGroupColor(2,1<<2,"255 0 0 255");         //Other Racers - Red

   //Setup Racers
   SetSensorGroupColor(3,~0,"255 255 255 255");   //Everyone Else - White
   SetSensorGroupColor(3,1<<3,"255 0 0 255");         //Other Racers - Red

   for(%i = Duels.minSensorGroup; %i <= Duels.MaxSensorGroup - 1; %i = %i +2)
   {
      %j = %i + 1;
      // set everyone else to black
      SetSensorGroupColor(%i,~0,"0 0 0 255");
      SetSensorGroupColor(%j,~0,"0 0 0 255");
      // set teamates to green
      SetSensorGroupColor(%i,1<<%i,"0 255 0 255");
      SetSensorGroupColor(%j,1<<%j,"0 255 0 255");
      // set Enemies to red
      SetSensorGroupColor(%i,1<<%j,"255 0 0 255");
      SetSensorGroupColor(%j,1<<%i,"255 0 0 255");
   
   }
   $GTest = true;
 // OK, here is where i need to set up my sensor groups colors.
 // Group 0 = invincible people.. No fire Zone/Snipe Range
 // Group 1 = standard Death Match
 // Group 2 = Racers
 // Group 3 = ?
 // Group 4 = ?
 // Group 5 - ? = Duelers


//   if ((%group = nameToID("MissionGroup/Teams")) == -1)
//     return;

//   %dropSet = new SimSet("TeamDrops0");
//   MissionCleanup.add(%dropSet);

//   %group.setTeam(0);

//   game.numTeams = 1;
//   setSensorGroupCount(32);

   //now set up the sensor group colors - specific for bounty - everyone starts out green to everone else...
//   for(%i = 0; %i < 32; %i++)
//      setSensorGroupColor(%i, 0xfffffffe, "0 255 0 255");
}
/////////////////////////////////////////////////////////////////////////////
//                          Programming functions                          //
/////////////////////////////////////////////////////////////////////////////
function resetsensors()
{
      %MaxS = Duels.maxSensorGroup;
   setSensorGroupCount(31);
   //Set up the invincible White Group and how they see everyone
   SetSensorGroupColor(0,~0,"0 0 0 255");         //All Duelers - Black
   SetSensorGroupColor(0,1<<0,"255 255 255 255"); //Same Group - White
   SetSensorGroupColor(0,1<<1,"255 0 0 255");     //Death Match - Red
   SetSensorGroupColor(0,1<<2,"0 0 255 255");     //Racers - Blue

   //Set up Standard DeathMatch
   SetSensorGroupColor(1,~0,"0 0 0 255");         //All Duelers - Black
   SetSensorGroupColor(1,1<<0,"255 255 255 255"); //Invincible - White
   SetSensorGroupColor(1,1<<1,"255 0 0 255");     //Death Match - Red
   SetSensorGroupColor(1,1<<2,"0 0 255 255");     //Racers - Blue

   //Setup Racers
   SetSensorGroupColor(2,~0,"255 255 255 255");   //Everyone Else - White
   SetSensorGroupColor(2,1<<2,"255 0 0 255");         //Other Racers - Red
   //5057.setsensorgroup(
//   for(%i = Duels.minSensorGroup; %i <= 30; %i = %i +2)
//   {
//      %j = %i + 1;
      // set everyone else to white
//      SetSensorGroupColor(%i,~%i,"0 0 0 255");
//      SetSensorGroupColor(%j,~%j,"0 0 0 255");
      // set teamates to green
//      SetSensorGroupColor(%i,1<<%i,"0 255 0 255");
//      SetSensorGroupColor(%j,1<<%j,"0 255 0 255");
      // set Enemies to red
//      SetSensorGroupColor(%i,1<<%j,"255 0 0 255");
//      SetSensorGroupColor(%j,1<<%i,"255 0 0 255");

//   }

}
function rebuild()
{
   $rebuilding = true;
   compile("scripts/TGGame.cs");
   exec("scripts/TGGame.cs");

   compile("scripts/TGStations.cs");
   exec("scripts/TGStations.cs");

   compile("scripts/TGTrigger.cs");
   exec("scripts/TGTrigger.cs");
   
   compile("scripts/TGSnipingRange.cs");
   exec("scripts/TGSnipingRange.cs");
   
   compile("scripts/TGOptSetter.cs");
   exec("scripts/TGOptSetter.cs");

   compile("scripts/TGRace.cs");
   exec("scripts/TGRace.cs");
   
      compile("scripts/TGDuel.cs");
   exec("scripts/TGDuel.cs");

   error("TGGame rebuilt");

   $rebuilding = false;
}


