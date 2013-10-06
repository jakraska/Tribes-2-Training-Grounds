//datablock TriggerData(stationTrigger)

//function stationTrigger::onEnterTrigger(%data, %obj, %colObj)
//function stationTrigger::onLeaveTrigger(%data, %obj, %colObj)
//function Station::stationTriggered(%data, %obj, %isTriggered)
//function stationTrigger::onTickTrigger(%data, %obj)

datablock TriggerData(TSSelectorTrigger)
{
   tickPeriodMS = 30;
};
datablock TriggerData(NoFireZone)
{
   tickPeriodMS = 30;
};
function NoFireZone::onEnterTrigger(%data, %obj, %colObj)
{

   //Allow for overlaping NoFireZones
   %colObj.NoFireZoneCount++;
   if(%colObj.InNoFireZone)
      return;

   if(%colObj.inDuel)
   {
      %velocity = %colobj.getvelocity();
      %velocity = VectorScale(%velocity, -1);
      %colobj.setVelocity(%velocity);
      messageClient(%colObj.client, 'ChatMessage', '\c5You are Currently in a duel~wfx/misc/warning_beep.wav');
       return;
   }
   if(%colObj.getState() $= "Dead")
      return;
   %colObj.InNoFireZone = true;
   messageClient(%colObj.client, 'ChatMessage', '\c5You are entering a NO-Fire Zone~wfx/misc/warning_beep.wav');
   //EXPERIMENT!
   error("setting Sensor group to zero of client:" SPC %Colobj.client);
   setTargetSensorGroup(%colObj.client.Target,0);
}
function NoFireZone::onLeaveTrigger(%data, %obj, %colObj)
{
   if(%colObj.inDuel)
      return;
   if(%colObj.getState() $= "Dead")
      return;

   //Allow for overlaping No fire zones
   %colObj.NoFireZoneCount--;
   if(%colObj.NoFireZoneCount != 0)
      return;

   %colObj.InNoFireZone = false;
   if(!%colObj.punishing)
      messageClient(%colObj.client, 'ChatMessage', '\c5You are leaving a NO-Fire Zone~wfx/misc/warning_beep.wav');
   if(!%colObj.DuelWait && !%colobj.InRace )
   {
      error("Leaving No fire and setting Sensor Group to 1");
      setTargetSensorGroup(%colObj.client.Target,1);
   }
}
function NoFireZone::onTickTrigger(%data, %obj)
{
}

function TSSelectorTrigger::onEnterTrigger(%data, %obj, %colObj)
{
  if(%colObj.isTransfering)
  {

     return;
  }

  %colObj.isTransfering = true;

  %colObj.TrasferState = 0;
   TSSelector::StationSparkEmitter(%colObj.client);
   %colObj.playAudio(0, FlashGrenadeExplosionSound);

 //make sure it's a player object, and that that object is still alive
   if(%colObj.getDataBlock().className !$= "Armor" || %colObj.getState() $= "Dead")
      return;

   %obj.station.schedule(500,"playThread",$ActivateThread,"flash");

   %colObj.inStation = true;

   if(Game.stationOnEnterTrigger(%data, %obj, %colObj)) {
      //verify station.team is team associated and isn't on player's team

   //Find the station that matches the Pad Number
   %PadNumber = %obj.station.PadNumber;
   for(%i = 0; %i < $TSSelector::Count; %i++)
   {


      if($TSSelector::PadNumber[%i] == %PadNumber && $TSSelector::PadID[%i] != %obj.station)
         break;
   }
   if(%i == $TSSelector::Count)
      return;

   %trans = $TSSelector::PadID[%i].Trigger.GetTransform();
   %X   = getword(%trans, 0);
   %Y   = getword(%trans, 1);
   %Z   = getword(%trans, 2) + 1;
   %rot = getword(%trans, 3) SPC getword(%trans, 4) SPC getword(%trans, 5) SPC getword(%trans, 6);

   %pos = %X SPC %Y SPC %Z SPC %rot;
   %colObj.setvelocity("0 0 0");

   %colObj.SetTransform(%pos);


//Now let's add the ability to call unique Functions for the pad types
   TSSelectorTrigger::Delegate(%obj, %colObj);



      


   }
}
function TSSelectorTrigger::Delegate(%trigger, %player)
{
   %type = %trigger.Station.type;

   
   switch$(%type)
   {
      case "SnipingSelector":
         TSSelectorTrigger::SnipingSelector(%trigger, %player);
      case "DeathMatchSelector":
         TSSelectorTrigger::DeathMatchSelector(%trigger, %player);

   }
}
function TSSelectorTrigger::DeathMatchSelector(%trigger, %player)
{
   %client = %player.client;

   if(%player.Client.TS == $TrainingStatus::Selecting)
   {
   //Set Sniping Changes
      %player.Client.TS = $TrainingStatus::DeathMatch;

      //Clear Invintory
      %player.clearInventory();
      %player.client.setWeaponsHudClearAll();


        buyFavorites(%client);
      //Set Invintory
//      %player.setInventory(EnergyPack,1);
//      %player.setInventory(SniperRifle,1);
//      %player.weaponCount = 1;
//      %player.use("SniperRifle");

      return;
   }
   if(%player.Client.TS == $TrainingStatus::DeathMatch)
   {
   //Revert DeathMatch Changes

      %player.Client.TS = $TrainingStatus::Selecting;

      //Clear Invintory
      %player.clearInventory();
      %player.client.setWeaponsHudClearAll();
      %player.setArmor("light");

      //Set Invintory
      %player.setInventory(EnergyPack,1);
      %player.setInventory(Beacon, 3);
      %player.setInventory(TargetingLaser, 1);
      %player.weaponCount = 0;
      %player.use("TargetingLaser");

      %player.setInventory(Disc,1);
      %player.setInventory(Discammo,50);
   }

}
function TSSelectorTrigger::SnipingSelector(%trigger, %player)
{
   %client = %player.client;

   if(%player.Client.TS == $TrainingStatus::Selecting)
   {
   //Set Sniping Changes
      %player.Client.TS = $TrainingStatus::Sniping;

      //Clear Invintory
      %player.clearInventory();
      %player.client.setWeaponsHudClearAll();

      //Set Invintory
      %player.setInventory(EnergyPack,1);
      %player.setInventory(SniperRifle,1);
      %player.weaponCount = 1;
      %player.use("SniperRifle");

      return;
   }
   if(%player.Client.TS == $TrainingStatus::Sniping)
   {
   //Revert Sniping Changes
      %player.Client.TS = $TrainingStatus::Selecting;

      //Clear Invintory
      %player.clearInventory();
      %player.client.setWeaponsHudClearAll();

      //Set Invintory
      %player.setInventory(EnergyPack,1);
      %player.setInventory(Beacon, 3);
      %player.setInventory(TargetingLaser, 1);
      %player.weaponCount = 0;
      %player.use("TargetingLaser");

      %player.setInventory(Disc,1);
      %player.setInventory(Discammo,50);

   }

}
function TSSelectorTrigger::onLeaveTrigger(%data, %obj, %colObj)
{
   if(%colObj.TrasferState == 0)
   {
      %colObj.TrasferState = 1;
      return;
   }
   if(%colObj.TrasferState == 1)
   {
      %colObj.isTransfering = false;

   }

   if(%colObj.getDataBlock().className !$= "Armor")
      return;

   %colObj.inStation = false;

   if(%obj.station)
   {
      if(%obj.station.triggeredBy == %colObj)
      {
         %obj.station.getDataBlock().stationFinished(%obj.station);
         %obj.station.getDataBlock().endRepairing(%obj.station);
         %obj.station.triggeredBy = "";
         %obj.station.getDataBlock().stationTriggered(%obj.station, 0);

         if(!%colObj.teleporting)
            %colObj.station = "";

      }
   }



}
function TSSelectorTrigger::stationTriggered(%data, %obj, %isTriggered)
{


   if(%isTriggered)
   {
      %obj.setThreadDir($ActivateThread, TRUE);
      %obj.playThread($ActivateThread,"activate");
      %obj.playAudio($ActivateSound, %data.getSound(true));
      %obj.inUse = "Up";
   }
   else
   {
      if(%obj.getDataBlock().getName() !$= StationVehicle)
      {
         %obj.stopThread($ActivateThread);
         if(%obj.getObjectMount())
            %obj.getObjectMount().stopThread($ActivateThread);
         %obj.inUse = "Down";
      }
      else
      {
         %obj.setThreadDir($ActivateThread, FALSE);
         %obj.playThread($ActivateThread,"activate");
         %obj.playAudio($ActivateSound, %data.getSound(false));
         %obj.inUse = "Down";
      }
   }
}
function TSSelectorTrigger::onTickTrigger(%data, %obj)
{
}

