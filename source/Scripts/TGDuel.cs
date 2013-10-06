////////////////////////////////////////////////////////////////////////////////
//                             Duel Station Data                              //
////////////////////////////////////////////////////////////////////////////////
datablock StaticShapeData(DuelBlock)
{
   className = "deadArmor";
   catagory = "Stations";
   shapeFile = "statue_base.dts";
   isInvincible = true;
};
datablock StaticShapeData(DuelStation) : StaticShapeDamageProfile
{
   className = Station;
   catagory = "Stations";
   shapeFile = "teamlogo_projector.dts";
   maxDamage = 1.00;
   destroyedLevel = 1.00;
   disabledLevel = 0.70;
   explosion      = ShapeExplosion;
	expDmgRadius = 8.0;
	expDamage = 0.4;
	expImpulse = 1500.0;
	// don't allow this object to be damaged in non-team-based
	// mission types (DM, Rabbit, Bounty, Hunters)
	noIndividualDamage = true;

   dynamicType = $TypeMasks::StationObjectType;
	isShielded = true;
	energyPerDamagePoint = 75;
	maxEnergy = 50;
	rechargeRate = 0.35;
   doesRepair = true;
   humSound = StationInventoryHumSound;

   cmdCategory = "Support";
   cmdIcon = CMDStationIcon;
   cmdMiniIconName = "commander/MiniIcons/com_inventory_grey";
   targetNameTag = 'Inventory';
   targetTypeTag = 'Station';

   debrisShapeName = "debris_generic.dts";
   debris = StationDebris;
};

datablock TriggerData(DuelStationTrigger)
{
   tickPeriodMS = 30;
};
function DuelStationTrigger::onEnterTrigger(%data, %obj, %colObj)
{
   if(%obj.station.occupied)
      return;
      

   %obj.station.player = %colObj;
   %msg = ":: Waiting for Opponents ::" NL "Duel Type:" SPC %obj.station.duelType;
   centerPrint( %colObj.client, %msg, 300, 2 );

   %group = %obj.station.group;
   %obj.station.occupied = true;
   //Activation and information special effects here
   
   %obj.station.StopThread($ambientThread);
   
   //check to see if other stations are occupied
   for(%i = 1; %i <= $DuelStation::GroupCount[%group]; %i++)
   {
      %CHK = $DuelStation::GroupID[%group, %i];
      if(!%CHK.occupied)
         return;
   }
   //If we made it to this point, all the stations are occupied...
   //Time to start the duel!
   //DuelStation::CreateDuel(%group, %obj.station.DuelType);
   duels.CreateDuel(%group);
}
function DuelStationTrigger::onLeaveTrigger(%data, %obj, %colObj)
{
    if(%obj.station.player != %colObj)
       return;

    %obj.station.player = "";
    %obj.station.occupied = false;
   %obj.station.playThread($AmbientThread, "ambient");
   clearCenterPrint(%colObj.client);
}
function DuelStationTrigger::onTickTrigger(%data, %obj)
{
}
function DuelBlock::onAdd(%this, %obj)
{
   %numDuelers = %obj.numDuelers;
   if($DuelBlock::GroupCount[%numDuelers] $= "")
      $DuelBlock::GroupCount[%numDuelers] = 0;


   //check to see if the group is already accounted for
   for(%i = 1; %i <= $DuelBlock::GroupCount[%numDuelers]; %i++)
   {
      if($DuelBlock::GroupID[%numDuelers, %i] == %obj.ID)
      {
         %Exists = true;
         break;
      }
   }
   if(!%Exists)
   {
      $DuelBlock::GroupCount[%numDuelers]++;
      $DuelBlock::GroupID[%numDuelers, $DuelBlock::GroupCount[%numDuelers]] = %obj.ID;
      // initialize the INUSE state
      $DuelBlock::GroupInUse[%numDuelers, $DuelBlock::GroupCount[%numDuelers]] = false;
   }
   // ok, our groups should be registerd.. now i need to add the individual blocks
   
   if($DuelBlock::BlockCount[%obj.ID] $= "")
      $DuelBlock::BlockCount[%obj.ID] = 0;
   $DuelBlock::BlockCount[%obj.ID]++;
   $DuelBlock::BlockID[%obj.ID, $DuelBlock::BlockCount[%obj.ID]] = %obj;

   
   Parent::onAdd(%this, %obj);


}

function DuelStation::onAdd(%this, %obj)
{
   // Do i need to track them all individually?
//   if($DuelStation::Count $= "")
//      $DuelStation::Count = 0;
//   $DuelStation::Count++;
//   $DuelStation::ID[$DuelStation::Count] = %obj;
   %group = %obj.group;
   if($DuelStation::GroupCount[%group] $= "")
      $DuelStation::GroupCount[%group] = 0;
   $DuelStation::GroupCount[%group]++;
   $DuelStation::GroupID[%group, $DuelStation::GroupCount[%group]] = %obj;

   Parent::onAdd(%this, %obj);
   %obj.playThread($AmbientThread, "ambient");

   %obj.setRechargeRate(%obj.getDatablock().rechargeRate);

   %trigger = new Trigger()
   {
      dataBlock = DuelStationTrigger;
      polyhedron = "-0.75 0.75 0.1 1.5 0.0 0.0 0.0 -1.5 0.0 0.0 0.0 2.3";
   };
   MissionCleanup.add(%trigger);
   %trigger.setTransform(%obj.getTransform());

   %trigger.station = %obj;
   %trigger.mainObj = %obj;
   %trigger.disableObj = %obj;
   %obj.trigger = %trigger;

}





new ScriptObject(Duels){
   //this is a bad way to set it up while i'm building the game.
   //if people are dueling when i rebuild, it will mess them up
   Class = DuelOrganizer;
   minSensorGroup = 4;
   maxSensorGroup = 32;


};
function DuelOrganizer::InitializeDuels(%this)
{
   error("initializing duels");
   for(%i = %this.minSensorGroup; %i <= %this.maxSensorGroup - 1; %i = %i + 2)
   {
      %this.Duel[%i] = new ScriptObject(){
                          Class = Duel;
                          Active = false;
                          ID = %i;
                          WinAwarded = false;
                       };
   }

}
// NOTE::  BAD BAD WAY TO DO THIS.  I SHOULD DO IT ON MAP LOAD INSTEAD
error("calling for initialization");
duels.InitializeDuels();


function DuelOrganizer::FindFreeDuelID(%this)
{
   for(%i = %this.minSensorGroup; %i <= %this.maxSensorGroup; %i = %i + 2)
   {

      if(!%this.Duel[%i].active)
      {
         error("found Duel ID =" SPC %i);
         return %i;
      }
   }
   //No free groups found
   return false;
}
function DuelOrganizer::OnClientKilled(%this, %clVictim, %clKiller, %clDammageType)
{
    error("Duels On killed is called");
    %this.Duel[%clVictim.player.DuelID].OnClientKilled(%clVictim, %clKiller, %clDammageType);

}
function DuelOrganizer::ClearDuel(%this, %ID)
{
   error("DuelOrganizer Class is Clearing Duel of ID:" SPC %ID);
   %this.Duel[%id].Clear();
   %this.Duel[%id] = new ScriptObject(){
                          Class = Duel;
                          Active = false;
                          ID = %id;
                          WinAwarded = false;
                       };

}
function DuelOrganizer::AbortCreation(%this, %ID)
{
   %Duel = %this.Duel[%id];
   for(%i = 1; %i <= %Duel.TotalPlayers; %i++)
   {
      %player.InDuel = false;
      %player.DuelWait = false;
      %player.DuelTeam = "";

   }
   %this.Duel[%ID] = new ScriptObject(){
                          Class = Duel;
                          Active = false;
                          ID = %ID;
                          WinAwarded = false;
                       };

}
function DuelOrganizer::CreateDuel(%this, %StationGroup)
{
   //IMPORTANT::  once i position the dulers, i lose access to them without
   //creating extra variables
   %DuelID = %this.FindFreeDuelID();
   if(!%DuelID)
   {
      error("No free Duel/Sensor Groups");
      return;

   }
   %Duel = %this.Duel[%duelID];
   %Duel.Active = true;
   %Duel.Type = $DuelStation::GroupID[%StationGroup, 1].DuelType;




    //duels.duel[5].schedule(50,"testschedule");

   switch$(%Duel.Type)
        {
            case "Special":




            default:
               for(%i = 1; %i <= $DuelStation::GroupCount[%StationGroup]; %i++)
                  {
                    %station = $DuelStation::GroupID[%StationGroup, %i];

                    %Duel.addPlayer(%station.player, %station.team);
                  }

                  if(!%duel.Position())
                  {
                    //make sure the duel isn't started if no blocks exist
                    error("Preventing Duel");
                    %this.AbortCreation(%DuelID);
                    return;
                  }
                  %duel.SetInventory();
                  %duel.Prepare();
                  %duel.ScheduleStart(%StationGroup);

        }

}
function Duel::SetActive(%this)
{
   %this.Active = true;
}
function Duel::AddPlayer(%this, %player, %team)
{
   if(%this.PlayerCount[%team] $= "")
   {
      %this.playerCount[%team] = 0;
      %this.LivingCount[%team] = 0;
      if(%this.TeamCount $= "")
         %this.TeamCount = 0;
      %this.TeamCount++;
   }
   if(%this.TotalPlayers $= "")
      %this.TotalPlayers = 0;

   %this.playerCount[%team]++;
   %this.LivingCount[%team]++;
   %this.player[%team, %this.playerCount[%team]] = %player;
   %this.TotalPlayers++;
   %this.player[%this.TotalPlayers] = %player;
   
   %player.DuelTeam = %team;
   %player.DuelID = %this.ID;
   %player.DuelWait = true;
   

}
function Duel::RemovePlayer(%this, %player)
{
   %team = %player.DuelTeam;
   %playerFound = false;
   for(%i = 1; %i <= %this.playerCount[%team]; %i++)
   {
      if(%this.player[%team, %i] == %player)
         %playerFound = true;

      if(%playerFound)
      {
        //now it's time to reorganize
        %this.player[%team, %i] = %this.player[%team, %i + 1];
      }
   
   }


   //avoid potential problem
    %this.player[%team, %this.playerCount[%team]] = "";

    %this.playerCount[%team]--;
    %this.LivingCount[%team]--;
    
    //revert the player back to normal just in case...
    %player.DuelWait = false;
    %player.InDuel = false;
}
function Duel::Position(%this)
{
   switch$(%this.Type)
        {
            case "Special":

            case "Shrike":
              // sorta random placement is the default
               //Store the num duelers
               %numDuelers = %this.TotalPlayers;

               //First pick what set of duelblocks to use

               //Start with an initial guess and incriment it so we don't get stuck
               // in an infinate loop

               %BlockGroup = mFloor(getRandom(1, $DuelBlock::GroupCount[%numDuelers]));
               %loopCount = 0;

               while( 1 < 100)
               {
                   %loopCount++;
                   if(%loopCount > $DuelBlock::GroupCount[%numDuelers])
                   {
                      error("ERROR: NO MORE DUEL BLOCKS LEFT");
                      return false;
                   }
                   // make sure it's not already being used...
                   if(!$DuelBlock::GroupInUse[%numDuelers, %BlockGroup])
                   {
                      $DuelBlock::GroupInUse[%numDuelers, %BlockGroup] = true;
                      %this.BlockGroup = %BlockGroup;
                      break;
                   }

                   //incriment our group
                   %BlockGroup++;
                   if(%BlockGroup > $DuelBlock::GroupCount[%numDuelers])
                      %BlockGroup = 1;

               }
               // place each player at a block ... Since each group
               //should have the same number of blocks as num of players...
               // i'll just place 1 at 1, 2 at 2 etc...
               for(%i = 1; %i <= %numDuelers; %i++)
               {
                  %player = %this.player[%i];
                  %GroupID = $DuelBlock::GroupID[%numDuelers, %BlockGroup];

                  %newOBJ = $DuelBlock::BlockID[%GroupID, %i];
                  error(%newOBJ);
                  %oldT = %newOBJ.GetTransform();
                  %NewZ = GetWord(%oldT,2) + 10;
                  %rot = GetWord(%oldT,6) +3.14;    //make em back to back
                  //%NewYrot = GetWord(%oldT,4)*-1;
                  %NewTransform = GetWords(%oldT,0,1) SPC %NewZ SPC GetWords(%oldT,3,5) SPC %rot;
                  error("OLD TRANSFORM:" SPC %player.getTransform());
                  error("NEW TRANSFORM:" SPC %NewTransform);
                  %this.CreateVehicle(%player.Client, "ScoutFlyer");
                  %vehicle = %player.client.vehicleMounted;
                  %vehicle.setVelocity("0 0 0");
                  %vehicle.setTransform(%NewTransform);


               }
               return true;

            default:
               // sorta random placement is the default
               //Store the num duelers
               %numDuelers = %this.TotalPlayers;

               //First pick what set of duelblocks to use

               //Start with an initial guess and incriment it so we don't get stuck
               // in an infinate loop

               %BlockGroup = mFloor(getRandom(1, $DuelBlock::GroupCount[%numDuelers]));
               %loopCount = 0;

               while( 1 < 100)
               {
                   %loopCount++;
                   if(%loopCount > $DuelBlock::GroupCount[%numDuelers])
                   {
                      error("ERROR: NO MORE DUEL BLOCKS LEFT");
                      return false;
                   }
                   // make sure it's not already being used...
                   if(!$DuelBlock::GroupInUse[%numDuelers, %BlockGroup])
                   {
                      $DuelBlock::GroupInUse[%numDuelers, %BlockGroup] = true;
                      %this.BlockGroup = %BlockGroup;
                      break;
                   }

                   //incriment our group
                   %BlockGroup++;
                   if(%BlockGroup > $DuelBlock::GroupCount[%numDuelers])
                      %BlockGroup = 1;

               }
               // place each player at a block ... Since each group
               //should have the same number of blocks as num of players...
               // i'll just place 1 at 1, 2 at 2 etc...
               for(%i = 1; %i <= %numDuelers; %i++)
               {
                  %player = %this.player[%i];
                  %GroupID = $DuelBlock::GroupID[%numDuelers, %BlockGroup];

                  %newOBJ = $DuelBlock::BlockID[%GroupID, %i];
                  error(%newOBJ);
                  %oldT = %newOBJ.GetTransform();
                  %NewZ = GetWord(%oldT,2) + 3.5;
                  %NewTransform = GetWords(%oldT,0,1) SPC %NewZ SPC GetWords(%oldT,3,6);
                  error("OLD TRANSFORM:" SPC %player.getTransform());
                  error("NEW TRANSFORM:" SPC %NewTransform);
                  %player.setVelocity("0 0 0");
                  %player.setTransform(%NewTransform);


               }
               return true;
        }
}

function Duel::ScheduleStart(%this, %StationGroup)
{
     %DammageDelay = 1000;
     %numDuelers = %this.TotalPlayers;
             for(%i = 1; %i <= %numDuelers; %i++)
             {
                %Client = %this.player[%i].client;
                $Duel::Countdown[%client,5] = %this.schedule(0, "notifyStart", %client, 5000);
                $Duel::Countdown[%client,4] = %this.schedule(1000, "notifyStart", %client, 4000);
                $Duel::Countdown[%client,3] = %this.schedule(2000, "notifyStart", %client, 3000);
                $Duel::Countdown[%client,2] = %this.schedule(3000, "notifyStart", %client, 2000);
                $Duel::Countdown[%client,1] = %this.schedule(4000, "notifyStart", %client, 1000);

                $Duel::Countdown[%client,0] = %this.schedule(5000, "FreeDueler", %client.player);

                $Duel::Countdown[%client,6] = %this.schedule(5000 + %DammageDelay, "StartDuel", %StationGroup);
             }
}
function Duel::CreateVehicle(%this,%client,%blockname)
{
  %obj = %blockName.create(%team);
   if(%obj)
   {

      %obj.team = 1;

      %station.playThread($ActivateThread,"activate2");
      %station.playAudio($ActivateSound, ActivateVehiclePadSound);

      //vehicleListAdd(%blockName, %obj);
      MissionCleanup.add(%obj);

     // %obj.schedule(3700, "playAudio", 0, VehicleAppearSound);

      %client.player.lastVehicle = %obj;
      %obj.lastPilot = %client.player;

      // play the FX
      %fx = new StationFXVehicle()
      {
         dataBlock = VehicleInvFX;
         stationObject = %station;
      };
     //mount the player as pilot
     %client.player.getDataBlock().onCollision(%client.player, %obj, 0);
  // if(%obj.getTarget() != -1)
     // setTargetSensorGroup(%obj.getTarget(), %client.getSensorGroup());
   %obj.DuelTeam = %client.player.DuelTeam;
   %obj.DuelID = %client.player.DuelID;
   %obj.DuelWait = %client.player.DuelWait;
   }
}
function Duel::SetInventory(%this)
{
   // use ShapeBase::setInventory(%this,%data,%value,%force) to set inv

    switch$(%this.Type)
       {
          case "Shrike":
             //I guess this is the best place to create the shrike
             %numDuelers = %this.TotalPlayers;
             for(%i = 1; %i <= %numDuelers; %i++)
             {
               %player = %this.Player[%i];
               %player.setDamageLevel(0);
               %player.setArmor("light");

               %player.ClearInventory();
               %player.client.setWeaponsHudClearAll();
               
               %player.setInventory(EnergyPack,1);
               %player.setInventory(TargetingLaser, 1);
               %player.setInventory(Disc,1);
               %player.setInventory(Discammo,15);
               %player.setInventory(RepairKit,1);

               %player.use("Disc");
              }
          case "Shock Lance Only":
             %numDuelers = %this.TotalPlayers;
             for(%i = 1; %i <= %numDuelers; %i++)
             {
               %player = %this.Player[%i];
               %player.setDamageLevel(0);
               
               %player.ClearInventory();
               %player.client.setWeaponsHudClearAll();

               %player.setInventory(EnergyPack,1);
               %player.setInventory(TargetingLaser, 1);
               %player.setInventory(ShockLance, 1);
               %player.setInventory(RepairKit,1);
               
               %player.use("ShockLance");


             }
         case "MA Disk":
             %numDuelers = %this.TotalPlayers;
             for(%i = 1; %i <= %numDuelers; %i++)
             {
               %player = %this.Player[%i];
               %player.setDamageLevel(0);

               %player.ClearInventory();
               %player.client.setWeaponsHudClearAll();

               %player.setInventory(EnergyPack,1);
               %player.setInventory(TargetingLaser, 1);
               %player.setInventory(Disc,1);
               %player.setInventory(Discammo,200, true);
               %player.setInventory(RepairKit,1);

               %player.use("Disc");


             }

          default:
             %numDuelers = %this.TotalPlayers;
             for(%i = 1; %i <= %numDuelers; %i++)
             {
               %player = %this.Player[%i];
               buyfavorites(%player.client);

               %player.setDamageLevel(0);
             }
        }
}
function Duel::Prepare(%this)
{
   // use ShapeBase::setInventory(%this,%data,%value,%force) to set inv

    switch$(%this.Type)
       {
          case "Special":

             // Set Target so only duelers can dammage each other
          case "Shrike":
          // setTargetSensorGroup(%obj.getTarget(), %client.getSensorGroup())
          %numDuelers = %this.TotalPlayers;
             for(%i = 1; %i <= %numDuelers; %i++)
             {
               %player = %this.Player[%i];
               //buyfavorites(%player.client);
               %vehicle = %player.client.vehicleMounted;
              // %player.setDamageLevel(0);
               //%player.setMoveState(true);
               %player.DuelWait = true;
               //%player.DuelGroup = %group;
              // %player.DuelTargetCount = %numDuelers - 1;
               //%player.DuelType = %DuelType;

               %vehicle.setFrozenState(true);
               %sensorGroup = %this.ID + %player.DuelTeam - 1;
               %player.client.SetSensorGroup(%sensorGroup);
               SetTargetSensorGroup(%player.client.Target, %SensorGroup);

               setTargetSensorGroup(%vehicle.getTarget(), %SensorGroup);
               error("Duel set sensor group to:" SPC %SensorGroup);

               %this.DisplayScore(%player.client, 5);
               //wtf? why won't this work?
               //centerprint(%player.client, "Your target is behind you... ATTACK!", 5000, 2);
               //error("centerprinting message v2");

             }
          default:
             %numDuelers = %this.TotalPlayers;
             for(%i = 1; %i <= %numDuelers; %i++)
             {
               %player = %this.Player[%i];
               //buyfavorites(%player.client);

              // %player.setDamageLevel(0);
               %player.setMoveState(true);
               %player.DuelWait = true;
               //%player.DuelGroup = %group;
              // %player.DuelTargetCount = %numDuelers - 1;
               //%player.DuelType = %DuelType;


               %sensorGroup = %this.ID + %player.DuelTeam - 1;
               %player.client.SetSensorGroup(%sensorGroup);
               SetTargetSensorGroup(%player.client.Target, %SensorGroup);
               error("Duel set sensor group to:" SPC %SensorGroup);
               %this.DisplayScore(%player.client, 5);
             }
       }


}
function Duel::notifyStart(%this, %client, %time)
{

   %seconds = mFloor(%time / 1000);


  if (%seconds < 30 && %seconds > 4)
      Messageclient(%client, 'ChatMessage', '\c2Duel starts in %1 seconds.~wfx/misc/hunters_%1.wav', %seconds);
   else if (%seconds == 4)
      Messageclient(%client, 'ChatMessage', '\c2Duel starts in 4 seconds. ~wfx/misc/hunters_4.wav');
   else if (%seconds == 3)
      Messageclient(%client, 'ChatMessage', '\c2Duel starts in 3 second. ~wfx/misc/hunters_3.wav');
   else if (%seconds == 2)
      Messageclient(%client, 'ChatMessage', '\c2Duel starts in 2 seconds. ~wvoice/announcer/ann.match_begins.wav');
   else if (%seconds == 1)
      Messageclient(%client, 'ChatMessage', '\c2Duel starts in 1 second.');
   //   UpdateClientTimes(%time);
}
function Duel::FreeDueler(%this, %player)
{
     switch$(%this.Type)
       {
          case "Shrike":
             %vehicle = %player.client.vehicleMounted;
             %vehicle.setFrozenState(false);
          default:
            %player.setMoveState(false);
       }

}
function Duel::StartDuel(%this, %StationGroup, %DuelType)
{
      switch$(%this.Type)
       {
          case "Special":

             // Set Target so only duelers can dammage each other
          case "Shrike":
           %numDuelers = %this.TotalPlayers;
           for(%i = 1; %i <= %numDuelers; %i++)
           {
             %player = %this.Player[%i];
             %vehicle = %player.client.vehicleMounted;
             %player.DuelWait = false;
             %vehicle.DuelWait = false;
             %player.InDuel = true;
             %vehicle.InDuel = true;
           }

           // allow other people to use the blocks now that they SHOULD be clear
             $DuelBlock::GroupInUse[%numDuelers,%this.blockgroup] = false;
          default:

             %numDuelers = %this.TotalPlayers;
             for(%i = 1; %i <= %numDuelers; %i++)
             {
                %player = %this.Player[%i];
                %player.DuelWait = false;
                %player.InDuel = true;
             }

             // allow other people to use the blocks now that they SHOULD be clear
             $DuelBlock::GroupInUse[%numDuelers,%this.blockgroup] = false;
         }
}
function IsMidAirHight(%player)
{
  //Taken from Evolution Mod... Thanks

  %minHight = 5;
  %posFrom = getWords( %player.getTransform(), 0, 2 );

  %posTo = VectorAdd( %posFrom, "0 0 -10000" );

  %hit1 = getWords( containerRayCast( %posFrom, %posTo,
                                      ( $TypeMasks::TerrainObjectType |
					$TypeMasks::StaticShapeObjectType |
					$TypeMasks::InteriorObjectType ),
                                      0),
		    1, 3);


  if ( VectorDist( %posFrom, %hit1 ) > ( %minHight ) )
    return 1;

  return 0;
}
function Duel::OnClientKilled(%this, %clVictim, %clKiller, %clDammageType)
{
   error("Duel on killed is called");
   switch$(%this.Type)
       {
          case "Special":

             // Set Target so only duelers can dammage each other

          default:
             %team = %clVictim.player.DuelTeam;
             %this.LivingCount[%team]--;
             if(%this.LivingCount[%team] <= 0 && !%this.WinAwarded)
             {
                %this.Results(%team);
                Duels.Schedule(3000, "ClearDuel", %this.ID);
             }
        }
}
function Duel::Results(%this, %loser)
{
  %this.WinAwarded = true;
  switch$(%this.Type)
       {
          case "Special":

             // Set Target so only duelers can dammage each other

          default:
            for(%i = 1; %i <= 2; %i++)
            {
               if(%i == %loser)
                  %Message = '\c5Your team LOST the Duel~wfx/misc/diagnostic_on.wav';
               else
                  %Message = '\c5Your team Won the Duel~wfx/misc/flag_capture.wav';
                  
               for(%j = 1; %j <= %this.PlayerCount[%i]; %j++)
               {
                   %client = %This.Player[%i, %j].client;
                   messageClient(%client, 'ChatMessage', %Message);
                   if(%i == %loser)
                      %client.DuelDeath[%this.Type]++;
                   else
                      %client.DuelWin[%this.Type]++;
                   %this.DisplayScore(%client, 6);
               }
            }
        }
}
function Duel::DisplayScore(%this, %client, %time)
{
  %Wins = mfloor(%client.DuelWin[%this.Type]);
  %Deaths = mfloor(%client.DuelDeath[%this.Type]);
  %Duels = %Deaths + %Wins;
  %acc = mfloor(%Wins / %Duels * 100);

  %message = "Duel Type: " SPC %this.Type NL "Wins:" SPC %Wins @ "    " @ "Deaths:" SPC %Deaths @ "    " @ "Accuracy:" SPC %acc SPC "%";
  bottomPrint( %client, %message, %time, 2 );

}
function Duel::Clear(%this)
{
   switch$(%this.Type)
       {
          case "Shrike":

             for(%i = 1; %i <= %this.TotalPlayers; %i++)
             {
                %player = %this.Player[%i];
                %vehicle = %player.lastvehicle;
                if(isobject(%vehicle))
                   %vehicle.setdamagelevel(1000); //kill it good :D
                // the pilot/crew should now automatically die
             }

          default:
             for(%i = 1; %i <= %this.TotalPlayers; %i++)
             {
                %player = %this.Player[%i];
                if(isobject(%player))
                   if(%player.getState() !$= "Dead")
                      %player.Scriptkill(0);
             }
   
       }

}

function TestDuel2()
{
 //$DuelBlock::GroupInUse[2,1] = false;
 duels.CreateDuel(1);

}

