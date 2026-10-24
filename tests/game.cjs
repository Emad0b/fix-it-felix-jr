/* Execute the real Scratch scripts, not a second implementation of the rules. */
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const VM = require('scratch-vm');
const {ScratchStorage} = require('scratch-storage');
const JSZip = require('jszip');

const root=path.resolve(__dirname,'..');
const wait=ms=>new Promise(resolve=>setTimeout(resolve,ms));
const successes=[];
let vm;
async function check(name,fn){await fn();successes.push(name);console.log('PASS '+name);}
function stage(){return vm.runtime.getTargetForStage();}
function get(name){return Number(stage().variables['g_'+name].value);}
function set(name,value){stage().variables['g_'+name].value=value;}
function message(name){vm.runtime.startHats('event_whenbroadcastreceived',{BROADCAST_OPTION:name});}
async function press(key,duration=60){vm.postIOData('keyboard',{key,isDown:true});await wait(duration);vm.postIOData('keyboard',{key,isDown:false});await wait(80);}
function stopRalph(){vm.runtime.stopForTarget(vm.runtime.targets.find(t=>t.getName()==='Ralph'));}
async function repair(){await press(' ');await wait(70);}

(async()=>{
  const data=fs.readFileSync(path.join(root,'Fix-It Felix Jr.sb3'));
  await check('Archive includes every costume and sound with matching checksum',async()=>{
    const zip=await JSZip.loadAsync(data);const project=JSON.parse(await zip.file('project.json').async('string'));
    assert.equal(project.targets.filter(t=>t.name.startsWith('Window ')).length,12);
    for(const target of project.targets){
      for(const asset of [...target.costumes,...target.sounds]){
        const bytes=await zip.file(asset.md5ext).async('nodebuffer');
        assert.equal(crypto.createHash('md5').update(bytes).digest('hex'),asset.assetId);
      }
      for(const [id,block] of Object.entries(target.blocks)){
        if(block.next){assert.ok(target.blocks[block.next]);assert.equal(target.blocks[block.next].parent,id);}
        for(const input of Object.values(block.inputs)){
          if(typeof input[1]==='string'){assert.ok(target.blocks[input[1]]);assert.equal(target.blocks[input[1]].parent,id);}
        }
      }
    }
  });
  vm=new VM();vm.attachStorage(new ScratchStorage());
  await vm.loadProject(data);vm.start();vm.greenFlag();await wait(350);stopRalph();
  await check('Green flag starts a complete 12-window level',async()=>{
    assert.equal(get('Score'),0);assert.equal(get('Lives'),3);assert.equal(get('Time'),60);
    assert.equal(get('Remaining'),12);assert.equal(get('Playing'),1);assert.equal(get('Level'),1);
  });
  await check('Held arrow keys move within all grid boundaries',async()=>{
    await press('ArrowRight',400);assert.equal(get('Column'),2);
    await press('ArrowLeft',500);assert.equal(get('Column'),0);
    await press('ArrowUp',600);assert.equal(get('Row'),3);
    await press('ArrowDown',650);assert.equal(get('Row'),0);
  });
  await check('Space repairs a window and awards points only once',async()=>{
    await repair();assert.equal(get('Score'),100);assert.equal(get('Remaining'),11);
    await repair();assert.equal(get('Score'),100);assert.equal(get('Remaining'),11);
    const window=vm.runtime.targets.find(t=>t.getName()==='Window 1');
    assert.equal(window.currentCostume,0);
  });
  await check('Ralph can break a repaired window without duplicating damage',async()=>{
    set('Damage',1);message('damage');await wait(80);assert.equal(get('Remaining'),12);
    message('damage');await wait(80);assert.equal(get('Remaining'),12);
    await repair();assert.equal(get('Score'),200);assert.equal(get('Remaining'),11);
  });
  await check('Finishing all windows advances the level and adds time',async()=>{
    const before=get('Time');
    for(let row=0;row<4;row++)for(let col=0;col<3;col++){
      set('Row',row);set('Column',col);message('repair');await wait(40);
    }
    await wait(100);
    assert.equal(get('Level'),2);assert.equal(get('Remaining'),12);assert.equal(get('Score'),1300);
    assert.ok(get('Time')>=before+18);
  });
  await check('A falling brick costs one life and enables temporary protection',async()=>{
    set('Row',3);set('Column',1);set('RalphColumn',1);set('Invincible',0);
    message('drop');await wait(330);
    assert.equal(get('Lives'),2);assert.equal(get('Invincible'),1);
    message('drop');await wait(330);assert.equal(get('Lives'),2);
    await wait(950);assert.equal(get('Invincible'),0);
  });
  await check('Zero lives ends the game and blocks further repairs',async()=>{
    set('Lives',0);await wait(80);assert.equal(get('Playing'),0);
    const before=get('Score');message('repair');await wait(60);assert.equal(get('Score'),before);
    assert.equal(vm.runtime.targets.find(t=>t.getName()==='Game over').visible,true);
  });
  await check('Green flag resets score, lives, windows and game-over banner',async()=>{
    vm.greenFlag();await wait(330);stopRalph();
    assert.equal(get('Score'),0);assert.equal(get('Lives'),3);assert.equal(get('Level'),1);
    assert.equal(get('Remaining'),12);assert.equal(get('Invincible'),0);
    assert.equal(vm.runtime.targets.find(t=>t.getName()==='Game over').visible,false);
  });
  await check('Countdown reaching zero ends the game',async()=>{
    set('Time',1);await wait(1150);assert.equal(get('Time'),0);assert.equal(get('Playing'),0);
  });
  fs.writeFileSync(path.join(root,'tests','results.json'),JSON.stringify({runtime:'scratch-vm 5.0.300',node:process.version,checks:successes,passed:successes.length,scope:'Actual .sb3 scripts executed headlessly. No renderer or audio output; visual/audio QA is separate.'},null,2));
  vm.quit();console.log(`\n${successes.length} Scratch runtime checks passed.`);process.exit(0);
})().catch(error=>{console.error(error);if(vm)vm.quit();process.exit(1);});
