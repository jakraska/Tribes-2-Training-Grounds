new ScriptObject(Races){
      Class = RaceOrganizer;
      minSensorGroup = 3;
      maxSensorGroup = 4;
      RaceCount = 0;
      lastSensorGroup = 2;

   };
datablock StaticShapeData(VehicleCKPT) : StaticShapeDamageProfile
{
   className = Station;
   catagory = "Stations";
   shapeFile = "nexus_effect.dts";
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

datablock StaticShapeData(RaceCheckpointCap) : StaticShapeDamageProfile
{
   className = Station;
   catagory = "Stations";
   shapeFile = "Stackable4l.dts";
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

function RaceOrganizer::Clear(%this)
{
   error("Clearing Races :::::::::: <<<<<<< :::::::");
   for(%i = 1; %i <= %this.RaceCount; %i++)
   {
      %this.RaceID[%i].Delete();
     // %this.freePadCount[%i] = 0;
     // %this.PadCount[%i] = 0;
   }
   %this.RaceCount = 0;
   %this.lastSensorGroup = %this.minSensorGroup - 1;
}

datablock TriggerData(RaceInitiateTrigger)
{
   tickPeriodMS = 30;
};
datablock TriggerData(RaceCkptTrigger)
{
   tickPeriodMS = 30;
};
datablock StaticShapeData(RaceInitiatePad) : StaticShapeDamageProfile
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
function Race::AddStartPad(%this, %pad)
{
   %group = %pad.group;
   //Store this array in the Race organizer object incase the race
   //hasn't actually been created yet
   if(%this.PadCount $= "")
      %this.PadCount = 0;
   %this.padID[%this.padCount] = %pad;
   %this.PadCount++;

   //free pad array.. used for random selection
   if(%this.freePadCount $= "")
      %this.freePadCount = 0;
   %this.freePadID[%this.freePadCount] = %pad;
   %this.freePadCount++;


}
function VehicleCKPT::onAdd(%this, %obj)
{
   Parent::onAdd(%this, %obj);
   %obj.playThread($AmbientThread, "ambient");
   if(!isobject(races.race[%obj.group]))
      races.CreateRace(%obj.group);

   Races.race[%obj.group].AddCKPT(%obj);

}
function RaceStartingPad::onAdd(%this, %obj)
{
   Parent::onAdd(%this, %obj);
   %obj.setRechargeRate(%obj.getDatablock().rechargeRate);

   if(!isobject(races.race[%obj.group]))
      Races.CreateRace(%obj.group);

   Races.race[%obj.group].AddStartPad(%obj);


}
function Race::AddCKPT(%this, %ckpt)
{
    %this.CheckPoint[%ckpt.ID] = %ckpt;
    if(%ckpt.isFinish)
       %this.CheckPointCount = %ckpt.ID;

}
function Race::CreateVehicle(%this,%client,%blockname)
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
   %obj.InRace = true;
   }
}
function Race::CKPTcheck(%this)
{
   if(%this.status !$= "InProgress" && %this.status !$= "ending")
      return;
   for(%i = 0; %i < %this.RacerCount; %i++)
   {
      %player = %this.racerClientID[%i].player;
      if(!isobject(%player))
         continue;
      %CKPT = %this.CheckPoint[%player.lastckpt + 1];
      
      %pX = GetWord(%player.GetTransform(), 0);
      %pY = GetWord(%player.GetTransform(), 1);
      
      %cX = GetWord(%CKPT.GetTransform(), 0);
      %cY = GetWord(%CKPT.GetTransform(), 1);
      
      %Dist = msqrt( mpow((%pX - %cX), 2) + mpow((%pY - %cY), 2) );
      
      if( %Dist <= %CKPT.CollisionRaidus )
         %this.EnterCheckpoint(%player, %CKPT);
   
   }
   //loop this function untill the race ends
   %this.schedule(50, "CKPTcheck");
}
function RaceOrganizer::CreateRace(%this, %group)
{
   //sould i make it so it won't create a race when there is
   //no free sensor group?  or should i just have the sensor groups
   //be messed?

//    if(races.LastSensorGroup > races.MaxSensorgroup)
//   {
//      error("Not enough sensor groups for a race.  Not creating the Trigger");

//      return;
//   }

   %this.RaceCount++;
   %this.Race[%group] = new ScriptObject(){
                          Class = Race;
                          Exists = true;
                          Group = %group;
                          SensorGroup = %this.LastSensorGroup;
                          StartTime = GetSimTime();
                          Status = "clear";
                          Parent = %this;
                          InitiatePad = "";
                          RacerCount = 0;
                       };
   %this.LastSensorGroup++;

   %this.RaceID[%this.RaceCount] = %this.Race[%group];
}
function Race::SetRaceInitiatePad(%this, %pad)
{
    %this.InitiatePad = %pad;
    %this.type = %pad.type;
    %this.name = %pad.RaceName;
}
function RaceInitiatePad::onAdd(%this, %obj)
{
     //New as of 9/13/03

   if(!isobject(races.race[%obj.group]))
      Races.CreateRace(%obj.group);

   races.race[%obj.group].SetRaceInitiatePad(%obj);



   Parent::onAdd(%this, %obj);
   %obj.playThread($AmbientThread, "Power");

   %obj.setRechargeRate(%obj.getDatablock().rechargeRate);

   %trigger = new Trigger()
   {
      dataBlock = RaceInitiateTrigger;
      polyhedron = "-0.75 0.75 0.1 1.5 0.0 0.0 0.0 -1.5 0.0 0.0 0.0 2.3";
   };
   MissionCleanup.add(%trigger);
   %trigger.setTransform(%obj.getTransform());

   %trigger.station = %obj;
   %trigger.mainObj = %obj;
   %trigger.disableObj = %obj;
   %obj.trigger = %trigger;
}
datablock StaticShapeData(RaceStartingPad) : StaticShapeDamageProfile
{
   className = Station;
   catagory = "Stations";
   shapeFile = "nexuscap.dts";
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
function RaceCkptTrigger::onTickTrigger(%data, %obj)
{
   %group = %obj.group;
   %race = races.race[%group];
   
   if(%race.type !$= "GravCycle")
      return;
   if(%race.status !$= "inprogress" && %race.status !$= "ending")
      return;
      
   //ok, a race is in progress and it's a grav cycle race... because
   //T2 is stupid and doesnt check for colisons of vehicles and triggers,
   //i need to do a container search and manually hit the on enter trigger
}
function RaceCkptTrigger::onEnterTrigger(%data, %obj, %colObj)
{

   if((%colObj.getDataBlock().className !$= "Armor" && %colObj.getDataBlock().className !$= "HoverVehicleData")|| %colObj.getState() $= "Dead")
      return;

   if(%colObj.getDataBlock().className $= "HoverVehicleData")
      %player = %colObj.lastPilot;
   else
      %player = %colObj;
      
   %ckptgroup = %obj.group;
   %racerGroup = %player.client.Racegroup;

   if(%ckptgroup != %racerGroup)
      return;

   %race = %player.Race;
   %race.EnterCheckpoint(%player, %obj);
}
function RaceCkptTrigger::onLeaveTrigger(%data, %obj, %colObj)
{
   //no errors
}
function Race::EnterCheckpoint(%this, %player, %ckpt)
{


   if((%player.lastckpt + 1) != %ckpt.ID)
      return;
   if(%ckpt.isFinish)
      %this.EndRaceForPlayer(%player);
   else
   {
      %player.lastckpt++;
      %time = GetSimTime() - %this.StartTime;
      %time2 = %this.convertTime(%time);

      messageClient(%player.client, 'ChatMessage', 'Your Current Time is\c5 %1~wfx/misc/diagnostic_on.wav', %time2);

   }
}
function Race::convertTime(%this, %time)
{
   %Fsec = %time / 1000;    //Float Seconds
   %Isec = mFloor(%Fsec);   //Int Seconds

   %MS = mFloor((%Fsec - %Isec) * 100);

   %min = mFloor(%Isec / 60);

   %sec = %Isec - (%min * 60);
   
   //Now lets make it look pretty
   if(%min < 10)
      %STRmin = "0" @ %min;
   else
      %STRmin = %min;
     
   if(%sec < 10)
      %STRsec = "0" @ %sec;
   else
      %STRsec = %sec;
      
   if(%MS < 10)
      %STRms = "0" @ %MS;
   else
      %STRms = %MS;
   
   return %STRmin @ ":" @ %STRsec @ ":" @ %STRms;
}
function RaceOrganizer::convertTime(%this, %time)
{
   %Fsec = %time / 1000;    //Float Seconds
   %Isec = mFloor(%Fsec);   //Int Seconds

   %MS = mFloor((%Fsec - %Isec) * 100);

   %min = mFloor(%Isec / 60);

   %sec = %Isec - (%min * 60);

   //Now lets make it look pretty
   if(%min < 10)
      %STRmin = "0" @ %min;
   else
      %STRmin = %min;

   if(%sec < 10)
      %STRsec = "0" @ %sec;
   else
      %STRsec = %sec;

   if(%MS < 10)
      %STRms = "0" @ %MS;
   else
      %STRms = %MS;

   return %STRmin @ ":" @ %STRsec @ ":" @ %STRms;
}
function Race::StartCountdown(%this, %TimeMS)
{
  %this.Countdown[0] = %this.schedule( %timeMS, "StartRace");
   if (%timeMS > 30000)
      %this.notifyRaceStart(%timeMS);

   if(%timeMS >= 30000)
       %this.Countdown[1] = %this.schedule(%timeMS - 30000, "notifyRaceStart", 30000);
   if(%timeMS >= 15000)
      %this.Countdown[2] = %this.schedule(%timeMS - 15000, "notifyRaceStart", 15000);
   if(%timeMS >= 10000)
     %this.Countdown[3] = %this.schedule(%timeMS - 10000, "notifyRaceStart", 10000);
   if(%timeMS >= 5000)
      %this.Countdown[4] = %this.schedule(%timeMS - 5000, "notifyRaceStart", 5000);
   if(%timeMS >= 4000)
      %this.Countdown[5] = %this.schedule(%timeMS - 4000, "notifyRaceStart", 4000);
   if(%timeMS >= 3000)
      %this.Countdown[6] = %this.schedule(%timeMS - 3000, "notifyRaceStart", 3000);
   if(%timeMS >= 2000)
      %this.Countdown[7] = %this.schedule(%timeMS - 2000, "notifyRaceStart", 2000);
   if(%timeMS >= 1000)
      %this.Countdown[8] = %this.schedule(%timeMS - 1000, "notifyRaceStart", 1000);
}
function Race::StartRace(%this)
{
   switch$(%this.Type)
       {
          case "GravCycle":
             %this.startTime = GetSimTime();

             for(%i = 0; %i < %this.racerCount; %i++)
             {
                %client = %this.racerClientID[%i];
                %player = %client.player;
                %vehicle = %client.vehicleMounted;
                %vehicle.setFrozenState(false);

                clearCenterPrint( %client );

                %client.lastckpt = 0;
                %this.CKPTcheck();
             }
          default:
             %this.startTime = GetSimTime();

             for(%i = 0; %i < %this.racerCount; %i++)
             {
                %client = %this.racerClientID[%i];
                %player = %client.player;

                clearCenterPrint( %client );
                %player.setMoveState(false);
                %client.lastckpt = 0;
             }

       }
}
function Race::Clear(%this)
{
  %group = %this.group;
  %this.MessageRacers('ChatMessage', 'Sorry, you took too long to finish the race.');
  for(%s = 0; %s <=8; %s++)
     cancel(%this.Countdown[%s]);


  %this.status = "clear";
  cancel(%this.Timeoutschedule[0]);
  cancel(%this.Timeoutschedule[1]);
  cancel(%this.Timeoutschedule[2]);

  for(%i = 0; %i < %this.racerCount; %i++)
  {
     %client = %this.racerClientID[%i];
     %this.racerClientID[%i] = "";

     clearCenterPrint( %client );
     %client.player.scriptKill(0);
  }
  %this.racerCount = 0;



   %count = %this.padCount;
   %this.freePadCount = %this.padCount;

   for(%p = 0; %p < %count; %p++)
   {
      %this.freePadID[%p] = %this.padID[%p];
   }


}

function Race::NotifyRaceStart(%this, %TimeMS)
{

   %seconds = mFloor(%TimeMS / 1000);
   %name = %this.name;
   if(%seconds == 5)
      %this.status = "inProgress";

   if (%seconds >= 30)
      MessageAll('ChatMessage', '\c2%2 starts in %1 seconds. Hurry up and join.~wfx/misc/hunters_%1.wav', %seconds, %name);
   if (%seconds < 30 && %seconds > 4)
      MessageAll('ChatMessage', '\c2%2 starts in %1 seconds.~wfx/misc/hunters_%1.wav', %seconds, %name);
   else if (%seconds == 4)
      %this.MessageRacers('ChatMessage', '\c2%1 starts in 4 seconds. ~wfx/misc/hunters_4.wav', %name);
   else if (%seconds == 3)
      %this.MessageRacers('ChatMessage', '\c2%1 starts in 3 seconds. ~wfx/misc/hunters_3.wav', %name);
   else if (%seconds == 2)
      %this.MessageRacers('ChatMessage', '\c2%1 starts in 2 seconds. ~wvoice/announcer/ann.match_begins.wav', %name);
   else if (%seconds == 1)
      %this.MessageRacers('ChatMessage', '\c2%1 starts in 1 second.', %name);

}
function Race::MessageRacers(%this, %msgType, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10, %a11, %a12)
{
   %group = %this.group;
   %count = %this.RacerCount;
   for(%cl = 0; %cl < %count; %cl++)
   {
      %client = %this.RacerClientID[%cl];
      messageClient(%client, %msgType, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10, %a11, %a12, %a13);
   }
}
function RaceOrganizer::EnterRace(%this, %player, %group)
{
   %race = %this.race[%group];
   if(%race.status $= "inprogress" || %race.status $= "ending")
   {
      messageClient(%player.client, 'ChatMessage', '\c5Race already in progress.  Please wait.~wfx/misc/diagnostic_on.wav');
      return;
   }
   if(%race.Status !$= "wait")
   {
      %race.StartCountdown(30000);
      %race.Status = "wait";
   }


   %race.AddPlayer(%player);
   
   %race.PositionPlayer(%player);
   %race.PreparePlayer(%player);
   
   if(%player.client.RaceRecord[%group] $= "")
      %ClRecord = "To be Determined";
   else
      %ClRecord = %this.convertTime(%player.client.RaceRecord[%group]);

   %BestRecord = $RaceRecord::Name[$CurrentMission, %group] @ ":  " SPC %this.convertTime($RaceRecord::Time[$CurrentMission, %group]);

   if(%race.type $= "GravCycle")
      %message = "Please wait for race to start." NL "Your fastest time:   " @ %ClRecord NL "Race record: " SPC %BestRecord;
   else
      %message = "Please wait for race to start. Loadout: E-Pack, 15 Disks, 1 Health kit." NL "Your fastest time:   " @ %ClRecord NL "Race record: " SPC %BestRecord;
   centerPrint( %player.client, %message, 60, 3 );


}
function Race::PreparePlayer(%this, %player)
{
      switch$(%this.Type)
        {
            case "GravCycle":
               %player.clearInventory();
               %player.client.setWeaponsHudClearAll();
               
               %player.setInventory(RepairPack,1);
               %player.setInventory(TargetingLaser, 1);

               %player.setDamageLevel(0.0);
               %vehicle = %player.client.vehicleMounted;
               %vehicle.setFrozenState(true);
               
            default:
               %player.clearInventory();
               %player.client.setWeaponsHudClearAll();

               //Set Invintory
               %player.setInventory(EnergyPack,1);
               %player.setInventory(TargetingLaser, 1);
               %player.setInventory(Disc,1);
               %player.setInventory(Discammo,15);
               %player.setInventory(RepairKit,1);
               %player.weaponCount = 1;
               %player.use("Disc");

               %player.setDamageLevel(0.0);
      
               %player.SetMoveState(true);
      }

}
function Race::AddPlayer(%this, %player)
{
   %this.racerClientID[%this.racerCount] = %player.client;
   %this.racerCount++;

   %player.InRace = true;
   %player.client.RaceGroup = %this.group;
   %player.Race = %this;

   %player.client.SetSensorGroup(%this.SensorGroup);
   SetTargetSensorGroup(%player.Client.Target, %this.SensorGroup);
}
function Race::PositionPlayer(%this, %player)
{
   %group = %player.client.RaceGroup;
   //let's find an empty starting pad
   if(%this.freePadCount <= 0)
   {
      messageClient(%player.client, 'ChatMessage', '\c5Sorry, this race is full.  Please wait for the next race.~wfx/misc/diagnostic_on.wav');
      return false;
   }

   %i = mFloor(getRandom(0, (%this.freePadCount - 1)));
   %pad = %this.freePadID[%i];
   %player.client.RacePad = %pad;

   //let's re-arrange the free pad array so we can use it again next time
   for(%destination = %i; %destination < %this.freePadCount; %destination++)
   {
      %source = %destination + 1;
      %this.freePadID[%destination] = %this.freePadID[%source];

   }
   %this.freePadCount--;
   
   //Set players position etc.
   %posXY = getWords(%pad.getTransform(),0 ,1);
   %posZ = getWord(%pad.getTransform(), 2);
   %rotang =  getWords(%pad.getTransform(), 3,6);


   switch$(%this.Type)
        {
           case "GravCycle":
              %this.CreateVehicle(%player.Client, "ScoutVehicle");
              %vehicle = %player.client.vehicleMounted;
   
              %vehicle.setvelocity("0 0 0");
              %vehicle.setTransform(%posXY SPC %posZ + 3.5 SPC %rotang);
           default:
              %player.Setvelocity("0 0 0");
              %player.setTransform(%posXY SPC %posZ + 0.5 SPC %rotang);
         }
}
function RaceInitiateTrigger::onEnterTrigger(%data, %obj, %colObj)
{
 //make sure it's a player object, and that that object is still alive
   if(%colObj.getDataBlock().className !$= "Armor" || %colObj.getState() $= "Dead")
      return;


   //%client = %colObj.client;
   %group = %obj.station.group;


  races.EnterRace(%colObj, %group);


}
function RaceInitiateTrigger::onLeaveTrigger(%data, %obj, %colObj)
{
}
function RaceInitiateTrigger::onTickTrigger(%data, %obj)
{
}
function RaceOrganizer::Delete(%this)
{
 error("calling Delete RaceOrganizer!!!!!!!!");
 Parent::Delete(%this);
}

function Race::EndRaceForPlayer(%this, %player)
{
      %client = %player.client;
      %group = %client.RaceGroup;
      %time = GetSimTime() - %this.StartTime;
      %time2 = %this.convertTime(%time);

    if(%time < %client.RaceRecord[%group] || %client.RaceRecord[%group] $= "")
      {
      %client.RaceRecord[%group] = %time;
      error("setting Client time");
      }

   if( %this.Status $= "inprogress" )
   {
      %this.Status = "Ending";
      MessageAll('chatmessage', '\c5%1 has won the %3 in %2~wfx/misc/flag_capture.wav', %client.namebase, %time2, %this.name);
      %this.LastAward = 1;
      if(%this.RacerCount > 1)
         %this.TimeoutCountdown();
      %player.scriptKill(0);

      if(%time < $RaceRecord::Time[$CurrentMission, %group] || $RaceRecord::Time[$CurrentMission, %group] $= "")
      {
         $RaceRecord::Time[$CurrentMission, %group] = %time;
         $RaceRecord::Name[$CurrentMission, %group] = gettaggedstring(%client.name);
         export( "$RaceRecord::*", "prefs/RaceRecords.cs", false );
      }

     return;
   }
   if( %this.status $= "Ending" )
   {
      %this.LastAward++;
      %place = %this.LastAward;
      MessageAll('ChatMessage', '\c5%1 finished the %4 in position %2 with a time of %3~wfx/misc/flag_capture.wav', %client.namebase, %place, %time2, %this.name);
      %player.scriptKill(0);
   }
//   Race::ClearRace(%group);


}
function Race::RemovePlayer(%this, %player)
{
   %client = %player.client;
   %group = %client.RaceGroup;
   if(%this.status $= "wait")
   {

      %this.freePadID[%this.freePadCount] = %clVictim.RacePad;
      %this.freePadCount++;
      clearCenterPrint(%client);

   }
   MessageAll('chatmessage', '%1 Died before the race finished.', %client.namebase, %time2, %this.name);
   %player.InRace = false;
   if(%this.Type $= "gravCycle" && isobject(%player.lastvehicle))
     %player.lastvehicle.setdamagelevel(1000);
   %client.RaceGroup = "";
   for(%i = 0; %i < %this.racerCount; %i++)
   {
      if(%client == %this.racerClientID[%i])
         break;
   }
   while(%i < %this.racerCount)
   {
      %source = %i + 1;
      %destination = %i;

      %this.racerClientID[%destination] = %this.racerClientID[%source];

      %i++;
   }

   if(%this.racerCount > 0)
      %this.racerCount--;


   if(%this.racerCount == 0)
   {

  //    if(%this.status $= "ending")
  //    {

  //    cancel(%this.Timeoutschedule[0]);
  //    cancel(%this.Timeoutschedule[1]);
  //    cancel(%this.Timeoutschedule[2]);
  //    }
      %this.Clear();
   }

}
