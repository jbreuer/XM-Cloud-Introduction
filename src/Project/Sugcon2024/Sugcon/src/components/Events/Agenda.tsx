import React from 'react';
import InnerHTML from 'dangerously-set-html-content';
import {
  Field,
  GetServerSideComponentProps, GetStaticComponentProps,
  useComponentProps,
  withDatasourceCheck
} from '@sitecore-jss/sitecore-jss-nextjs';
import useSWR from 'swr';
import { ComponentProps } from 'lib/component-props';

interface Fields {
  SessionizeUrl: Field<string>;
}

type AgendaProps = ComponentProps & {
  fields: Fields;
};

const AgendaDefaultComponent = (props: AgendaProps): JSX.Element => (
    <div className={`component promo ${props.params.styles}`}>
      <div className="component-content">
        <span className="is-empty-hint">Agenda</span>
      </div>
    </div>
);

const AgendaComponent = (props: AgendaProps): JSX.Element => {
  const id = props.params.RenderingIdentifier;

  console.log('props.rendering.uid', props.rendering.uid);
  
  const componentProps = useComponentProps(props.rendering.uid) || {};

  console.dir(componentProps, { depth: null });

  const fetcher = (url: string) => fetch(url).then((res) => res.text());
  const { data, error } = useSWR(
      props.fields.SessionizeUrl.value ? props.fields.SessionizeUrl.value : null,
      fetcher
  );

  if (!props?.fields?.SessionizeUrl?.value) {
    return <AgendaDefaultComponent {...props} />;
  }

  //TODO: design error
  if (error) {
    return <div>Failed to load...</div>;
  }

  //TODO: design loading
  if (!data) {
    return <div>Loading</div>;
  }

  return (
      <div className={`component agenda ${props.params.styles}`} id={id ? id : undefined}>
        <div className="component-content">
          <InnerHTML html={data} />
        </div>
      </div>
  );
};

export const getServerSideProps: GetServerSideComponentProps = async (_, layoutData, context) => {
  console.log('getServerSideProps');
  // console.log(layoutData, layoutData);
  // console.log(context, context);
  return {
    assetDetailsServer: 'Agenda test server',
  };
};

export const getStaticProps: GetStaticComponentProps = async (_, layoutData, context) => {
  console.log('getStaticProps');
  // console.log(layoutData, layoutData);
  // console.log(context, context);
  return {
    assetDetailsStatic: 'Agenda test',
  };
};

export const Default = withDatasourceCheck()<AgendaProps>(AgendaComponent);

