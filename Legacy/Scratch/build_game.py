"""Build an editable Scratch 3 arcade game using original vector pixel artwork."""
import argparse
import hashlib
import io
import json
import math
from pathlib import Path
import wave
import zipfile

ASSETS = {}
TARGETS = []
GLOBALS = {name: 'g_'+name for name in ['Score','Lives','Time','Level','Remaining','Playing','Column','Row','Invincible','Damage','RalphColumn']}
BROADCASTS = {name:'message_'+name for name in ['repair','damage','drop','hit','next level','game over']}


def costume(name, body, width, height):
    svg=f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}" shape-rendering="crispEdges">{body}</svg>'.encode()
    digest=hashlib.md5(svg).hexdigest();ASSETS[digest+'.svg']=svg
    return dict(name=name,assetId=digest,md5ext=digest+'.svg',dataFormat='svg',bitmapResolution=1,rotationCenterX=width/2,rotationCenterY=height/2)


def sound(name, frequencies, duration=.08):
    rate=22050;frames=bytearray()
    for hz in frequencies:
        for i in range(int(rate*duration)):
            envelope=(1-i/(rate*duration))*.18
            sample=int(32767*envelope*(1 if math.sin(2*math.pi*hz*i/rate)>0 else -1))
            frames.extend(sample.to_bytes(2,'little',signed=True))
    buffer=io.BytesIO()
    with wave.open(buffer,'wb') as output:
        output.setnchannels(1);output.setsampwidth(2);output.setframerate(rate);output.writeframes(frames)
    data=buffer.getvalue();digest=hashlib.md5(data).hexdigest();ASSETS[digest+'.wav']=data
    return dict(name=name,assetId=digest,md5ext=digest+'.wav',dataFormat='wav',format='',rate=rate,sampleCount=len(frames)//2)


class Sprite:
    def __init__(self,name,costumes,stage=False,layer=1):
        self.name=name;self.blocks={};self.i=0;self.locals={}
        self.target=dict(isStage=stage,name=name,variables={},lists={},broadcasts={value:key for key,value in BROADCASTS.items()} if stage else {},blocks=self.blocks,
            comments={},currentCostume=0,costumes=costumes,sounds=[],volume=35,layerOrder=layer)
        if stage:self.target.update(tempo=60,videoTransparency=50,videoState='off',textToSpeechLanguage=None)
        else:self.target.update(visible=True,x=0,y=0,size=100,direction=90,draggable=False,rotationStyle="don't rotate")
        TARGETS.append(self.target)

    def local(self,name,value=0):
        ident=self.name+'_'+name;self.locals[name]=ident;self.target['variables'][ident]=[name,value]

    def block(self,opcode,inputs=None,fields=None,shadow=False):
        self.i+=1;ident=f'{self.name}_{self.i}'
        record=dict(opcode=opcode,next=None,parent=None,inputs={},fields=fields or {},shadow=shadow,topLevel=False)
        for key,value in (inputs or {}).items():
            if isinstance(value,str) and value in self.blocks:
                record['inputs'][key]=[1 if self.blocks[value]['shadow'] else 2,value]
                self.blocks[value]['parent']=ident
            else:record['inputs'][key]=[1,[4 if isinstance(value,(int,float)) else 10,str(value)]]
        self.blocks[ident]=record;return ident

    def variable(self,name):return self.block('data_variable',fields={'VARIABLE':[name,self.locals.get(name,GLOBALS.get(name))]})
    def set(self,name,value):return self.block('data_setvariableto',{'VALUE':value},{'VARIABLE':[name,self.locals.get(name,GLOBALS.get(name))]})
    def change(self,name,value):return self.block('data_changevariableby',{'VALUE':value},{'VARIABLE':[name,self.locals.get(name,GLOBALS.get(name))]})
    def op(self,opcode,a,b):
        keys=('OPERAND1','OPERAND2') if opcode in ['equals','lt','gt','and','or'] else ('NUM1','NUM2')
        return self.block('operator_'+opcode,{keys[0]:a,keys[1]:b})
    def eq(self,a,b):return self.op('equals',a,b)
    def wait(self,seconds):return self.block('control_wait',{'DURATION':seconds})
    def chain(self,items,parent=None):
        items=[item for item in items if item]
        for index,item in enumerate(items):
            self.blocks[item]['parent']=parent if index==0 else items[index-1]
            self.blocks[item]['next']=items[index+1] if index+1<len(items) else None
        return items[0] if items else None
    def control(self,kind,body,condition=None):
        inputs={}
        if condition is not None:inputs['CONDITION']=condition
        ident=self.block('control_'+kind,inputs)
        head=self.chain(body,ident)
        if head:self.blocks[ident]['inputs']['SUBSTACK']=[2,head]
        return ident
    def when(self,kind,body,value=None):
        fields={}
        if kind=='keypressed':fields={'KEY_OPTION':[value,None]}
        if kind=='broadcastreceived':fields={'BROADCAST_OPTION':[value,BROADCASTS[value]]}
        hat=self.block('event_'+(kind if kind.startswith('when') else 'when'+kind),fields=fields);self.blocks[hat].update(topLevel=True,x=(self.i%4)*280,y=(self.i//4)*90)
        if body:self.blocks[hat]['next']=self.chain(body,hat)
        return hat
    def broadcast(self,name):
        ident=self.block('event_broadcast');self.blocks[ident]['inputs']['BROADCAST_INPUT']=[1,[11,name,BROADCASTS[name]]];return ident
    def costume(self,name):return self.block('looks_switchcostumeto',{'COSTUME':self.block('looks_costume',fields={'COSTUME':[name,None]},shadow=True)})
    def play(self,name):return self.block('sound_play',{'SOUND_MENU':self.block('sound_sounds_menu',fields={'SOUND_MENU':[name,None]},shadow=True)})
    def goto(self,x,y):return self.block('motion_gotoxy',{'X':x,'Y':y})
    def show(self):return self.block('looks_show')
    def hide(self):return self.block('looks_hide')
    def alive(self):return self.eq(self.variable('Playing'),1)


def rect(x,y,w,h,color):return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{color}"/>'
def text(x,y,value,size=12,color='#f8df87'):return f'<text x="{x}" y="{y}" text-anchor="middle" fill="{color}" font-family="monospace" font-weight="bold" font-size="{size}">{value}</text>'


def build(output):
    sky=rect(0,0,480,360,'#111a34')
    for x,y in [(20,80),(39,145),(440,100),(453,190),(27,245),(415,44)]:sky+=rect(x,y,2,2,'#a1b9d2')
    for x,height in [(0,95),(25,125),(420,140),(450,100)]:
        sky+=rect(x,330-height,30,height,'#1a2945')
    sky+=text(240,18,'FIX-IT FELIX JR.',17)+rect(68,115,344,228,'#4b2636')+rect(78,121,324,216,'#a55446')
    for y in range(122,337,12):
        sky+=rect(78,y,324,2,'#713c3f')
        for x in range(78+(12 if y%24 else 0),402,28):sky+=rect(x,y,2,12,'#713c3f')
    for y in [115,174,228,282,336]:sky+=rect(63,y,354,6,'#e6b578')+rect(63,y+6,354,4,'#6d3c41')
    sky+=rect(0,346,480,14,'#18223a')+text(240,357,'ARROWS: MOVE   SPACE: REPAIR   GREEN FLAG: RESTART',10,'#b3c8e1')
    stage=Sprite('Stage',[costume('Building',sky,480,360)],stage=True,layer=0)
    initial={'Lives':3,'Time':60,'Level':1,'Remaining':12,'Column':1,'RalphColumn':1}
    for name,ident in GLOBALS.items():stage.target['variables'][ident]=[name,initial.get(name,0)]
    stage.when('whenflagclicked',[
        stage.set('Playing',0),stage.set('Score',0),stage.set('Lives',3),stage.set('Time',60),stage.set('Level',1),
        stage.set('Remaining',12),stage.set('Column',1),stage.set('Row',0),stage.set('Invincible',0),stage.set('RalphColumn',1),
        stage.wait(.1),stage.set('Playing',1),
        stage.control('forever',[
            stage.control('if',[
                stage.control('if',[stage.set('Remaining',12),stage.change('Level',1),stage.change('Time',20),stage.broadcast('next level')],stage.op('and',stage.eq(stage.variable('Remaining'),0),stage.op('and',stage.op('gt',stage.variable('Time'),0),stage.op('gt',stage.variable('Lives'),0)))),
                stage.control('if',[stage.set('Playing',0),stage.broadcast('game over')],stage.op('or',stage.op('lt',stage.variable('Time'),1),stage.op('lt',stage.variable('Lives'),1)))
            ],stage.alive()),stage.wait(.03)
        ])
    ])
    stage.when('whenflagclicked',[stage.wait(.2),stage.control('forever',[stage.wait(1),stage.control('if',[stage.change('Time',-1)],stage.alive())])])

    normal=rect(0,0,66,50,'#e9b979')+rect(5,4,56,41,'#172d47')+rect(9,8,21,30,'#5597b0')+rect(36,8,21,30,'#5597b0')+rect(30,4,6,41,'#f0ca86')+rect(5,23,56,4,'#f0ca86')
    broken=normal+rect(10,9,18,12,'#122236')+rect(38,28,17,13,'#122236')+'<path d="M18 8 L25 17 L20 23 M48 8 L42 19 L49 24 M13 28 L23 35 L18 42" fill="none" stroke="#e4eff0" stroke-width="3"/>'
    for row in range(4):
        for col in range(3):
            index=row*3+col+1
            window=Sprite(f'Window {index}',[costume('Repaired',normal,66,50),costume('Broken',broken,66,50)],layer=1)
            window.local('Broken',1)
            window.target.update(x=(col-1)*110,y=-128+54*row,currentCostume=1)
            window.when('whenflagclicked',[window.set('Broken',1),window.costume('Broken'),window.show()])
            condition=window.op('and',window.alive(),window.op('and',window.eq(window.variable('Broken'),1),window.op('and',window.eq(window.variable('Column'),col),window.eq(window.variable('Row'),row))))
            window.when('broadcastreceived',[window.control('if',[window.set('Broken',0),window.costume('Repaired'),window.change('Score',100),window.change('Remaining',-1)],condition)],'repair')
            window.when('broadcastreceived',[window.control('if',[window.set('Broken',1),window.costume('Broken'),window.change('Remaining',1)],window.op('and',window.alive(),window.op('and',window.eq(window.variable('Damage'),index),window.eq(window.variable('Broken'),0))))],'damage')
            window.when('broadcastreceived',[window.set('Broken',1),window.costume('Broken')],'next level')

    felix=rect(8,2,18,5,'#68bdf4')+rect(5,7,25,5,'#3285be')+rect(10,12,16,11,'#ffcb92')+rect(24,15,5,5,'#ffcb92')+rect(21,14,3,3,'#192338')+rect(8,23,20,12,'#369ada')+rect(4,25,5,10,'#ffcb92')+rect(27,25,5,10,'#ffcb92')+rect(9,35,7,9,'#204b91')+rect(21,35,7,9,'#204b91')+rect(6,42,11,4,'#513c3a')+rect(21,42,11,4,'#513c3a')
    hammer=rect(29,14,4,16,'#c58d49')+rect(24,10,14,7,'#ffe76d')
    player=Sprite('Felix',[costume('Idle',felix,40,48),costume('Hammer',felix+hammer,40,48)],layer=5)
    player.target['sounds']=[sound('Repair',[660,880,1100],.035),sound('Ouch',[180,120],.09),sound('Level up',[523,659,784,1047],.06)]
    player.target.update(x=0,y=-128)
    player.when('whenflagclicked',[player.show(),player.costume('Idle'),player.block('looks_cleargraphiceffects'),player.control('forever',[
        player.goto(player.op('multiply',player.op('subtract',player.variable('Column'),1),110),player.op('add',-128,player.op('multiply',player.variable('Row'),54))),player.wait(.02)])])
    movement=[]
    for key,variable,delta,bound in [('right arrow','Column',1,2),('left arrow','Column',-1,0),('up arrow','Row',1,3),('down arrow','Row',-1,0)]:
        within=player.op('lt' if delta>0 else 'gt',player.variable(variable),bound)
        keydown=player.block('sensing_keypressed',{'KEY_OPTION':player.block('sensing_keyoptions',fields={'KEY_OPTION':[key,None]},shadow=True)})
        movement.append(player.control('if',[player.change(variable,delta)],player.op('and',player.alive(),player.op('and',within,keydown))))
    player.when('whenflagclicked',[player.control('forever',movement+[player.wait(.14)])])
    player.when('keypressed',[player.control('if',[player.broadcast('repair'),player.costume('Hammer'),player.play('Repair'),player.wait(.12),player.costume('Idle')],player.alive())],'space')
    player.when('broadcastreceived',[player.play('Ouch'),player.block('looks_seteffectto',{'VALUE':55},{'EFFECT':['GHOST',None]}),player.wait(1.1),player.block('looks_cleargraphiceffects'),player.set('Invincible',0)],'hit')
    player.when('broadcastreceived',[player.play('Level up'),player.block('looks_sayforsecs',{'MESSAGE':player.block('operator_join',{'STRING1':'LEVEL ','STRING2':player.variable('Level')}),'SECS':1})],'next level')

    ralphart=rect(9,0,24,9,'#713c30')+rect(7,9,28,16,'#e7a276')+rect(10,7,8,4,'#713c30')+rect(26,7,8,4,'#713c30')+rect(13,14,4,3,'#282033')+rect(27,14,4,3,'#282033')+rect(19,23,9,3,'#492337')+rect(3,27,38,22,'#ce5a42')+rect(0,27,8,26,'#e7a276')+rect(38,27,8,26,'#e7a276')+rect(8,49,12,14,'#67403f')+rect(27,49,12,14,'#67403f')+rect(4,59,17,6,'#e7a276')+rect(26,59,17,6,'#e7a276')
    ralph=Sprite('Ralph',[costume('Ralph',ralphart,46,66)],layer=4);ralph.target.update(x=0,y=98)
    ralph.when('whenflagclicked',[ralph.show(),ralph.wait(.7),ralph.control('forever',[
        ralph.control('if',[ralph.set('RalphColumn',ralph.block('operator_random',{'FROM':0,'TO':2})),ralph.goto(ralph.op('multiply',ralph.op('subtract',ralph.variable('RalphColumn'),1),110),98),
            ralph.set('Damage',ralph.op('add',ralph.op('add',ralph.op('multiply',ralph.block('operator_random',{'FROM':0,'TO':3}),3),ralph.variable('RalphColumn')),1)),
            ralph.broadcast('damage'),ralph.broadcast('drop')],ralph.alive()),
        ralph.wait(ralph.op('divide',1.5,ralph.op('add',1,ralph.op('multiply',ralph.variable('Level'),.15))))
    ])])

    brick=Sprite('Brick',[costume('Brick',rect(0,0,20,11,'#3e2735')+rect(2,2,16,7,'#da7351')+rect(2,2,16,2,'#f5ad6c')+rect(9,3,2,6,'#9b4c3d'),20,11)],layer=6)
    brick.target['visible']=False
    brick.when('whenflagclicked',[brick.hide()])
    clone_menu=brick.block('control_create_clone_of_menu',fields={'CLONE_OPTION':['_myself_',None]},shadow=True)
    brick.when('broadcastreceived',[brick.control('if',[brick.block('control_create_clone_of',{'CLONE_OPTION':clone_menu})],brick.alive())],'drop')
    # Clone entry hat is a control opcode rather than an event opcode.
    clonehat=brick.block('control_start_as_clone');brick.blocks[clonehat].update(topLevel=True,x=400,y=80)
    x=brick.block('motion_xposition');y=brick.block('motion_yposition')
    dx=brick.block('operator_mathop',{'NUM':brick.op('subtract',x,brick.op('multiply',brick.op('subtract',brick.variable('Column'),1),110))},{'OPERATOR':['abs',None]})
    dy=brick.block('operator_mathop',{'NUM':brick.op('subtract',y,brick.op('add',-128,brick.op('multiply',brick.variable('Row'),54)))},{'OPERATOR':['abs',None]})
    collision=brick.op('and',brick.op('lt',dx,19),brick.op('lt',dy,25))
    can_hit=brick.op('and',collision,brick.eq(brick.variable('Invincible'),0))
    loop=brick.control('repeat_until',[
        brick.block('motion_changeyby',{'DY':brick.op('subtract',0,brick.op('add',3,brick.op('multiply',brick.variable('Level'),.65)))}),
        brick.control('if',[brick.set('Invincible',1),brick.change('Lives',-1),brick.broadcast('hit'),brick.block('control_delete_this_clone')],can_hit),brick.wait(.03)
    ],brick.op('or',brick.op('lt',brick.block('motion_yposition'),-150),brick.eq(brick.variable('Playing'),0)))
    brick.blocks[clonehat]['next']=brick.chain([brick.goto(brick.op('multiply',brick.op('subtract',brick.variable('RalphColumn'),1),110),65),brick.show(),loop,brick.block('control_delete_this_clone')],clonehat)

    bannerart=rect(0,0,370,125,'#0d1329')+rect(4,4,362,117,'#efbf70')+rect(9,9,352,107,'#1b233e')+text(185,47,'GAME OVER',29)+text(185,78,'CLICK THE GREEN FLAG TO TRY AGAIN',13,'#a9c8e9')+text(185,101,'REPAIR. DODGE. REPEAT.',12,'#dc8b77')
    banner=Sprite('Game over',[costume('Game over',bannerart,370,125)],layer=9)
    banner.target['visible']=False
    banner.when('whenflagclicked',[banner.hide()]);banner.when('broadcastreceived',[banner.goto(0,0),banner.show()],'game over')
    monitors=[]
    for name,x,y in [('Score',7,27),('Lives',135,27),('Time',245,27),('Level',355,27),('Remaining',7,55)]:
        monitors.append(dict(id=GLOBALS[name],mode='default',opcode='data_variable',params={'VARIABLE':name},spriteName=None,value=0,width=0,height=0,x=x,y=y,visible=True,sliderMin=0,sliderMax=100,isDiscrete=True))
    project=dict(targets=TARGETS,monitors=monitors,extensions=[],meta={'semver':'3.0.0','vm':'15.3.0','agent':'Original procedural Scratch arcade project'})
    output.mkdir(parents=True,exist_ok=True)
    (output/'project.json').write_text(json.dumps(project,indent=2))
    assetsdir=output/'assets';assetsdir.mkdir(exist_ok=True)
    for name,data in ASSETS.items():(assetsdir/name).write_bytes(data)
    with zipfile.ZipFile(output/'Fix-It Felix Jr.sb3','w',zipfile.ZIP_DEFLATED) as archive:
        archive.writestr('project.json',json.dumps(project))
        for name,data in ASSETS.items():archive.writestr(name,data)
    print(f'Built {len(TARGETS)} targets, {sum(len(t["blocks"]) for t in TARGETS)} blocks, {len(ASSETS)} original assets.')


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--output',type=Path,default=Path(__file__).resolve().parent)
    build(parser.parse_args().output)
